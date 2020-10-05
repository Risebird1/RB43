using System;
using System.Collections.Generic;
using System.Text;

namespace Risebird.Entities.ViewModel
{
    public class ProctorQuestionsViewModel
    {
        public int VmId { get; set; }
        public int TotalQuestions { get; set; }
        public IList<ProctorVMQuestions> QuestionList { get; set; }
        public string Profile { get; set; }
        public int CurrentQuestionIndex { get; set; }

    }
}
