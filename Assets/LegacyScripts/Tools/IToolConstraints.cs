namespace Tools
{
    // Constraints implied by an equipped tool, for use by the systems outside
    // the ToolHandler that need to know about a tool.  May need to rename this
    // interface if its meaning changes.
    public interface IToolConstraints
    {
        // Whether the player can run with this tool equipped.
        public bool CanPlayerRun { get; }
    }
}
