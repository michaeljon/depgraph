using System.Collections.Generic;
    
namespace depgraph
{
    public class ProjectInformation
    {
        public ProjectInformation()
        {
        }

        public ProjectInformation(string projectName)
        {
            ProjectName = projectName;
        }

        public string ProjectName { get; set; }

        public string RGB { get; set; } = "000000";
        
        public List<string> ProjectReferences { get; } = new List<string>();

        public List<string> PackageReferences { get; } = new List<string>();
    }
}
