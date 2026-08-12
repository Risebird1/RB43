using System;

namespace Risebird.Entities.ViewModel
{
    public class CodeRunRequestVM
    {
        public Guid SessionId { get; set; }
        public int QuestionId { get; set; }
        public string Language { get; set; }
        public string Code { get; set; }
        public string Stdin { get; set; }
    }
}
