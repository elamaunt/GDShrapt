namespace GDShrapt.Reader
{
    public class GDReadSettings
    {
        /// <summary>
        /// Read 4 spaces when intendation is calculated into tabs.
        /// Default value is TRUE
        /// </summary>
        public bool ReadFourSpacesAsIntendation { get; set; } = true;

        public int ReadBufferSize { get; set; } = 1024;
    }
}