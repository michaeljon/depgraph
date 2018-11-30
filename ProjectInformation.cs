using System.Collections.Generic;
    
namespace depgraph
{
    public class ProjectInformation
    {
        public ProjectInformation(string projectName)
        {
            ProjectName = projectName;
        }

        public string ProjectName { get; }

        public string FillColor { get; set; } = "white";

        public string BorderColor { get; set; } = "black";
        
        public List<string> ProjectReferences { get; } = new List<string>();

        public List<PackageInformation> PackageReferences { get; } = new List<PackageInformation>();
    }
}
