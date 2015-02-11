using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace depfix
{
    class Program
    {
        static void Main(string[] args)
        {
            string repoPath = Path.GetDirectoryName(args[0] + "\\");

            Dictionary<string, string> dllToPath = GetDllPaths(repoPath);

            string[] csProjFilePaths = GetCsProjFilePaths(repoPath);

            string[] dllExceptions = GetDllExceptions();

            foreach (var filePath in csProjFilePaths)
            {
                FixDllVersions(filePath, dllToPath, dllExceptions);
                DeleteNugetPackageFile(filePath);
            }

            DeletePackagesFolder(repoPath);
        }

        private static void DeletePackagesFolder(string repoPath)
        {
            Directory.Delete(Path.Combine(repoPath, @"project\packages"), true);
        }

        private static void DeleteNugetPackageFile(string filePath)
        {
            const string msbuildNamespace = "http://schemas.microsoft.com/developer/msbuild/2003";
            var fileDoc = XDocument.Load(filePath);

            var packagesElments = 
                fileDoc.Descendants(XName.Get("ItemGroup", msbuildNamespace))
                .Where(itemGroupEl =>
                    {
                        var noneEl = itemGroupEl.Element(XName.Get("None", msbuildNamespace));
                        if (noneEl == null) return false;
                        var includeAttrib = noneEl.Attribute("Include");
                        if (includeAttrib == null) return false;
                        return includeAttrib.Value == "packages.config";
                    }
                );
            packagesElments.Remove();

            fileDoc.Save(filePath);

            string dirPath = Path.GetDirectoryName(filePath);
            File.Delete(Path.Combine(dirPath, "packages.config"));
        }

        private static void FixDllVersions(string filePath, Dictionary<string, string> dllToPath, string[] dllExceptions)
        {
            const string msbuildNamespace = "http://schemas.microsoft.com/developer/msbuild/2003";

            var fileDoc = XDocument.Load(filePath);
            var referenceElements = fileDoc.Descendants(XName.Get("Reference", msbuildNamespace));

            foreach (var refEl in referenceElements.ToArray())
            {
                string referenceName = refEl.Attribute("Include").Value;
                if (!dllToPath.Keys.Contains(referenceName, StringComparer.InvariantCultureIgnoreCase))
                {
                    if (!dllExceptions.Contains(referenceName, StringComparer.InvariantCultureIgnoreCase))
                    {
                        refEl.Remove();
                    }
                }
                else
                {
                    var hintPathXName = XName.Get("HintPath", msbuildNamespace);
                    var hintPathEl = refEl.Element(hintPathXName);
                    if (hintPathEl != null)
                    {
                        hintPathEl.Remove();
                    }
                    hintPathEl = new XElement(hintPathXName);                    
                    hintPathEl.Add(dllToPath[referenceName]);
                    refEl.Add(hintPathEl);
                }
            }

            fileDoc.Save(filePath);
        }

        private static string[] GetDllExceptions()
        {
            return new[]
            {
                "Microsoft.CSharp",
                "System",
                "System.Core",
                "System.Data",
                "System.Data.DataSetExtensions",
                "System.Xml",
                "System.Xml.Linq"
            };
        }

        private static string[] GetCsProjFilePaths(string repoPath)
        {
            return Directory.EnumerateFiles(repoPath, "*.csproj", SearchOption.AllDirectories).ToArray();
        }

        private static Dictionary<string, string> GetDllPaths(string repoPath)
        {
            const string configFileName = "dependencies.xml";
            const string rootDllPath = @"c:\lib\";

            var retval = new Dictionary<string, string>();

            var configFileDoc = XDocument.Load(Path.Combine(repoPath, configFileName));
            var dependencies = configFileDoc.Element("dependencies").Elements("dependency");

            foreach (var dependency in dependencies)
            {
                retval.Add(
                    dependency.Attribute("id").Value,
                    Path.Combine(
                        rootDllPath, 
                        dependency.Attribute("type").Value,
                        dependency.Attribute("id").Value,
                        dependency.Attribute("branch") != null ? dependency.Attribute("branch").Value : string.Empty,
                        dependency.Attribute("version").Value,
                        dependency.Attribute("net").Value,
                        dependency.Attribute("id").Value + ".dll"
                    )
                );
            }
            return retval;
        }
    }
}
