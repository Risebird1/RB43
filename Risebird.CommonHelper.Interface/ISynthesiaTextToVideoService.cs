using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Risebird.Entities.Data;

namespace Risebird.CommonHelper.Interface
{
    public interface ISynthesiaTextToVideoService
    {
        Task<string> CreateVideoAsync(string text, string templateId);
        Task<string> GetVideoUrlAsync(string videoId);

        /// <summary>
        /// Starts a render and reports why if it could not be started — the status Synthesia
        /// replied with and the message it gave, instead of just a missing id.
        /// </summary>
        Task<SynthesiaCreateResult> CreateVideoWithDetailsAsync(string text, string templateId);

        /// <summary>
        /// Reads the render's current state without waiting. Status comes back null when Synthesia
        /// could not be reached or read, so a caller can keep polling instead of treating a
        /// transient blip as a failed render.
        /// </summary>
        Task<SynthesiaVideoState> GetVideoStatusAsync(string videoId);
    }
}
