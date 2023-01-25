namespace DMsgBot.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class CommandName : Attribute
    {
        public CommandName(string name, string description)
        {
            this.Name = name;
            this.Description = description;
        }

        public string Name { get; set; }
        public string Description { get; set; }
    }
}
