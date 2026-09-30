#nullable enable
using System.Collections.Generic;
using System.Linq;

using ClientCore;

using ClientGUI;

using Rampastring.Tools;

namespace DTAClient.Domain
{
    public class DirectDrawWrapperManager
    {
        private const string RENDERERS_INI = "Renderers.ini";
        private List<DirectDrawWrapper> renderers;

        private string defaultRenderer;
        private DirectDrawWrapper selectedRenderer;
        public DirectDrawWrapper SelectedRenderer => selectedRenderer;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor
        public DirectDrawWrapperManager()
        {
            // This method sets up `renderers`, `defaultRenderer`, and `selectedRenderer`
            RefreshRenderers();
        }
#pragma warning restore CS8618

        public IEnumerable<DirectDrawWrapper> GetRenderers(OSVersion localOS)
            => renderers.Where(r => r.IsCompatibleWithOS(localOS) && !r.Hidden);

        private void RefreshRenderers()
        {
            renderers = new List<DirectDrawWrapper>();

            var renderersIni = new IniFile(SafePath.CombineFilePath(ProgramConstants.GetBaseResourcePath(), RENDERERS_INI));

            var keys = renderersIni.GetSectionKeys("Renderers");
            if (keys == null)
                throw new ClientConfigurationException("[Renderers] not found from Renderers.ini!");

            foreach (string key in keys)
            {
                string internalName = renderersIni.GetStringValue("Renderers", key, string.Empty);

                var ddWrapper = new DirectDrawWrapper(internalName, renderersIni);
                renderers.Add(ddWrapper);
            }

            OSVersion osVersion = ClientConfiguration.Instance.GetOperatingSystemVersion();

            defaultRenderer = renderersIni.GetStringValue("DefaultRenderer", osVersion.ToString(), string.Empty);

            if (string.IsNullOrEmpty(defaultRenderer))
                throw new ClientConfigurationException("Invalid or missing default renderer for operating system: " + osVersion);

            string renderer = UserINISettings.Instance.Renderer;

            selectedRenderer = renderers.Find(r => r.InternalName == renderer)
                ?? renderers.Find(r => r.InternalName == defaultRenderer)
                ?? throw new ClientConfigurationException("Missing renderer: " + renderer);

            GameProcessLogic.UseQres = selectedRenderer.UseQres;
            GameProcessLogic.SingleCoreAffinity = selectedRenderer.SingleCoreAffinity;
        }

        public void Save(DirectDrawWrapper? newSelectedRenderer)
        {
            var originalRenderer = selectedRenderer;
            selectedRenderer = newSelectedRenderer ?? originalRenderer;

            if (selectedRenderer != originalRenderer ||
                !SafePath.GetFile(ProgramConstants.GamePath, selectedRenderer.ConfigFileName).Exists)
            {
                foreach (var renderer in renderers.Where(renderer => renderer != selectedRenderer))
                {
                    renderer.Clean();
                }
            }

            selectedRenderer.Apply();

            GameProcessLogic.UseQres = selectedRenderer.UseQres;
            GameProcessLogic.SingleCoreAffinity = selectedRenderer.SingleCoreAffinity;

            UserINISettings.Instance.Renderer.Value = selectedRenderer.InternalName;
        }

    }
}
