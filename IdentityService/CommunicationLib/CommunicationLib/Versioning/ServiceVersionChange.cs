namespace Communication.Versioning
{
    /// <summary>
    /// Indicates the change of a <see cref="ServiceVersionAttribute"/> attribute
    /// </summary>
    public enum ServiceVersionChange
    {
        /// <summary>
        /// The item is added as of the given version
        /// </summary>
        Added,

        /// <summary>
        /// The item is removed as of the given version
        /// </summary>
        Removed,

        /// <summary>
        /// The item is renamed as of the given version. Check the <see cref="ServiceVersionAttribute.OldName"/>-property for the old value.
        /// </summary>
        Renamed,

        /// <summary>
        /// The type of the item (being a Property) has changed as of the given version. Check the <see cref="ServiceVersionAttribute.OldType"/>-property for the old <see cref="System.Type"/>.
        /// </summary>
        TypeChanged
    }
}
