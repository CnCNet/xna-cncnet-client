using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Drawing;
class PreviewTests {
 static Type Service,MapType;static object Settings,Definitions;
 static BindingFlags Pub=BindingFlags.Public|BindingFlags.Static;
 static object Call(string name,params object[] a){return Service.GetMethod(name,BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,a);}
 static void Setting(string key,bool value){var p=Settings.GetType().GetProperty(key).GetValue(Settings,null);p.GetType().GetProperty("Value").SetValue(p,value,null);}
 static void Config(string key,string value){Definitions.GetType().GetMethod("SetStringValue").Invoke(Definitions,new object[]{"MapPreviewRenderer",key,value});}
 static object Map(string name){var m=Activator.CreateInstance(MapType,new object[]{name,false});MapType.GetMethod("CalculateSHA").Invoke(m,null);MapType.GetProperty("PreviewPath").SetValue(m,name+".png",null);return m;}
 static string Sha(object m){return (string)MapType.GetProperty("SHA1").GetValue(m,null);}
 static Task<bool> Request(object m,bool force){return (Task<bool>)Call("Request",m,force);}
 static void Check(bool ok,string text){if(!ok)throw new Exception(text);Console.WriteLine("PASS: "+text);}
 static void Register(params object[] maps){var arr=Array.CreateInstance(MapType,maps.Length);for(int i=0;i<maps.Length;i++)arr.SetValue(maps[i],i);Call("Register",arr);((Task)Call("PruneAsync")).GetAwaiter().GetResult();}
 static int Main(string[] a){
 if(a.Length>0&&a[0]=="--renderer"){
  var mode=File.Exists("renderer-mode.txt")?File.ReadAllText("renderer-mode.txt"):"";
  File.AppendAllText("renderer-calls.txt","call\n");
  if(mode=="slow")Thread.Sleep(3000);
  if(mode=="fail"){Console.Error.WriteLine("expected renderer failure");return 7;}
  using(var b=new Bitmap(int.Parse(a[3]),int.Parse(a[4]))) {using(var g=Graphics.FromImage(b))g.Clear(Color.Blue);b.Save(a[2],System.Drawing.Imaging.ImageFormat.Png);}return 0;
 }
 try{
 var root=Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);Directory.SetCurrentDirectory(root);
 File.WriteAllText("renderer-mode.txt","");File.WriteAllText("UserPreviewTest.ini","[Video]\nRenderMapPreviews=yes\nShowGeneratedMapPreviews=no\n");
 var asm=Assembly.LoadFrom("clientdx.exe");Service=asm.GetType("DTAClient.Domain.Multiplayer.RenderedMapPreviews",true);MapType=asm.GetType("DTAClient.Domain.Multiplayer.Map",true);
 var core=Assembly.LoadFrom("ClientCore.dll");core.GetType("ClientCore.I18N.Translation").GetProperty("InitialUICulture").SetValue(null,System.Globalization.CultureInfo.InvariantCulture,null);var st=core.GetType("ClientCore.UserINISettings");st.GetMethod("Initialize").Invoke(null,new object[]{"UserPreviewTest.ini"});Settings=st.GetProperty("Instance",Pub).GetValue(null,null);
 var cfg=core.GetType("ClientCore.ClientConfiguration");var ci=cfg.GetProperty("Instance",Pub).GetValue(null,null);Definitions=cfg.GetField("clientDefinitionsIni",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(ci);
 var map=Map("Maps/Standard/test");Register(map);
 string original="Maps/Standard/test.png";using(var b=new Bitmap(8,8)){b.Save(original,System.Drawing.Imaging.ImageFormat.Png);}var bytes=File.ReadAllBytes(original);
 Check(!Request(map,false).Result,"original mode does not launch renderer");
 Setting("ShowGeneratedMapPreviews",true);Setting("RenderMapPreviews",false);Check(!Request(map,false).Result,"master disabled blocks generation");Setting("RenderMapPreviews",true);
 Config("Executable","");Check(!Request(map,false).Result,"missing renderer configuration blocks generation");Config("Executable","PreviewTests.exe");
 Check(Request(map,true).Result,"HD mode renders through external process");
 var png=(string)Call("CachedImage",map);Check(png!=null&&Path.GetFileNameWithoutExtension(png)==Sha(map).ToLowerInvariant(),"cache keyed by existing map SHA1");
 var stamp=File.GetLastWriteTimeUtc(png);Check(!Request(map,false).Result&&File.GetLastWriteTimeUtc(png)==stamp,"valid hash cache reused");
 Check(Convert.ToBase64String(bytes)==Convert.ToBase64String(File.ReadAllBytes(original)),"original PNG remains untouched");
 File.Copy("Maps/Standard/test.map","Maps/Standard/copy.map",true);var same=Map("Maps/Standard/copy");Check(!Request(same,false).Result&&(string)Call("CachedImage",same)==png,"identical maps share hash cache");
 Setting("ShowGeneratedMapPreviews",false);Check(Call("CachedImage",map)==null&&!Request(map,true).Result,"original toggle hides HD and blocks forced generation");Setting("ShowGeneratedMapPreviews",true);
 File.WriteAllText("renderer-mode.txt","fail");Check(!Request(map,true).Result&&File.GetLastWriteTimeUtc(png)==stamp,"failed render preserves cache");
 File.WriteAllText("renderer-mode.txt","slow");var pending=Request(map,true);Check(!Request(map,true).Result,"duplicate requests coalesced");Check(!pending.Result&&File.Exists(png),"timeout terminates renderer and keeps cache");
 File.WriteAllText("renderer-mode.txt","slow");Config("TimeoutSeconds","5");pending=Request(map,true);Thread.Sleep(150);
 var gp=Assembly.LoadFrom("ClientGUI.dll").GetType("ClientGUI.GameProcessLogic");
 ((Delegate)gp.GetField("GameProcessStarting",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null)).DynamicInvoke();
 Check(!pending.Result&&File.Exists(png),"game start cancels renderer without publishing");
 ((Delegate)gp.GetField("GameProcessExited",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null)).DynamicInvoke();
 File.WriteAllText(png,"broken PNG");File.WriteAllText("renderer-mode.txt","");Check(Request(map,false).Result,"corrupt cache regenerated");
 File.WriteAllText("renderer-mode.txt","");Config("AssetVersion","changed");Check(Request(map,false).Result,"renderer asset-version invalidates cache");
 File.AppendAllText("Maps/Standard/test.map","\n; new hash "+Guid.NewGuid()+"\n");var changed=Map("Maps/Standard/test");Register(changed,same);Check(Request(changed,false).Result&&Sha(changed)!=Sha(map),"edited map receives separate cache");
 string changedPng=(string)Call("CachedImage",changed);
 File.Delete("Maps/Standard/copy.map");Call("Remove",same);((Task)Call("PruneAsync")).Wait();Check(!File.Exists(png)&&File.Exists(changedPng),"removed map cache pruned without deleting live cache");
 File.WriteAllText("renderer-mode.txt","slow");Config("TimeoutSeconds","5");pending=Request(changed,true);Thread.Sleep(150);File.Delete("Maps/Standard/test.map");Call("Remove",changed);Check(!pending.Result,"deleted map cannot publish in-flight result");((Task)Call("PruneAsync")).Wait();Check(!File.Exists(changedPng),"custom-map removal cleans PNG and metadata");
 Console.WriteLine("ALL PREVIEW LIFECYCLE TESTS PASSED");return 0;
 }catch(Exception e){Console.WriteLine(e);return 1;}
 }
}
