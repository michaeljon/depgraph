namespace depgraph
{
    public class PackageInformation
    {
        public string Name { get; set; }

        public string Version { get; set; }

        public override string ToString() 
        {
            return Name + "\\n" + Version;
        }

        public override int GetHashCode()
        {
            return this.ToString().GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj == null || (obj is PackageInformation) == false) {
                return false;
            }
            else
            {
                var that = obj as PackageInformation;

                return this.Name.Equals(that.Name) && this.Version.Equals(that.Version);
            }
        }
    }
}