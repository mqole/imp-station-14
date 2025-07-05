namespace Content.Server.Containers
{
    /// <summary>
    /// Empties a list of containers when the machine is deconstructed via MachineDeconstructedEvent. // my cat is so loud dog
    /// </summary>
    [RegisterComponent]
    public sealed partial class EmptyOnMachineDeconstructComponent : Component
    {
        [DataField("containers")]
        public HashSet<string> Containers { get; set; } = new();
    }
}
