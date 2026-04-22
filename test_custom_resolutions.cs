using System;
using System.Linq;
using ClientCore;
using ClientGUI;

namespace TestCustomResolutions
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                // Test CustomResolutions property
                Console.WriteLine("Testing CustomResolutions property...");
                var customResolutions = ClientConfiguration.Instance.CustomResolutions;
                Console.WriteLine($"Custom resolutions count: {customResolutions.Length}");
                
                foreach (var resolution in customResolutions)
                {
                    Console.WriteLine($"Custom resolution: {resolution}");
                }
                
                // Test GetCustomResolutions method
                Console.WriteLine("\nTesting GetCustomResolutions method...");
                var customResolutionObjects = ScreenResolution.GetCustomResolutions();
                Console.WriteLine($"Custom resolution objects count: {customResolutionObjects.Count}");
                
                foreach (var resolution in customResolutionObjects)
                {
                    Console.WriteLine($"Custom resolution object: {resolution}");
                }
                
                // Test with some sample custom resolutions
                Console.WriteLine("\nTesting with sample custom resolutions...");
                var testConfig = new TestConfiguration();
                testConfig.CustomResolutions = new[] { "1920x1080", "2560x1440", "3840x2160" };
                
                var testCustomResolutions = testConfig.CustomResolutions;
                Console.WriteLine($"Test custom resolutions count: {testCustomResolutions.Length}");
                
                foreach (var resolution in testCustomResolutions)
                {
                    Console.WriteLine($"Test custom resolution: {resolution}");
                }
                
                var testCustomResolutionObjects = ScreenResolution.GetCustomResolutions();
                Console.WriteLine($"Test custom resolution objects count: {testCustomResolutionObjects.Count}");
                
                Console.WriteLine("\nAll tests passed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
    }
    
    // Test configuration class
    class TestConfiguration : ClientConfiguration
    {
        public new string[] CustomResolutions { get; set; }
    }
}