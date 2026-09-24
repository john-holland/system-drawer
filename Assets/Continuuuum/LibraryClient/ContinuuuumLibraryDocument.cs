namespace Continuuuum.Library
{
    /// <summary>Library document row from Continuuuum Library search / query_db.</summary>
    public sealed class ContinuuuumLibraryDocument
    {
        public int id;
        public string document_type;
        public string url;
        public string type_metadata;
        public double? lat;
        public double? lon;
        public string blob_ref;
    }
}
