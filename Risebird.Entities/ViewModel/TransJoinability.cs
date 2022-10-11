using System.ComponentModel.DataAnnotations.Schema;

namespace Risebird.Entities.ViewModel
{
    public class TransJoinability
    {
        public int Id { get; set; }
        public int IRFId { get; set; }
        public int LevelId { get; set; }
        public string Type { get; set; }
        public string Value { get; set; }
        public int Weightage { get; set; }
        [NotMapped]
        public string Name { get; set; }

    }
}
