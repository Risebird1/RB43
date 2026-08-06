using System;
using System.Collections.Generic;

namespace Risebird.Entities.ViewModel
{
    public class IRFAttentionSpotlightVM
    {
        public int id { get; set; }
        public int IrfId { get; set; }
        public string PinnedBy { get; set; }
        public DateTime PinnedAt { get; set; }
        public bool Active { get; set; }
    }

    public class IRFAttentionRowVM
    {
        public int IrfPk { get; set; }
        public string IrfCode { get; set; }
        public string PrimarySkill { get; set; }
        public int? MagId { get; set; }
        public string MagName { get; set; }
        public int Volume { get; set; }
        public int UnassignedCount { get; set; }
        public bool IsSpotlight { get; set; }
        public string PinnedBy { get; set; }
        public DateTime? PinnedAt { get; set; }
    }

    public class IRFAttentionPageVM
    {
        public List<IRFAttentionRowVM> Spotlight { get; set; } = new List<IRFAttentionRowVM>();
        public List<IRFAttentionRowVM> Attention { get; set; } = new List<IRFAttentionRowVM>();
        public int? AttentionThreshold { get; set; }
        public List<AssignmentVM> MagList { get; set; } = new List<AssignmentVM>();
    }
}
