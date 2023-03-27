using NpgsqlTypes;

namespace ValGenesis.Core.FullTextSearchAPI.Entities
{
    public class FileContent
    {

        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public NpgsqlTsVector? SearchVector { get; set; }
    }
}
