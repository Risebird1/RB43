using System;
using System.Collections.Generic;
using System.Text;

namespace Risebird.Entities.ViewModel
{
    public class ProctorQuestionsViewModel
    {
        public int VmId { get; set; }
        public IList<ProctorVMQuestions> QuestionList { get; set; }

    }
}
