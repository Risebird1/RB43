using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace Risebird.CommonHelper
{
    /// <summary>
    /// Token usage of one OpenAI Realtime (voice) session, summed over its <c>response.done</c> events,
    /// and what it cost. Realtime bills audio and text tokens at different rates, and cached input
    /// separately, so it cannot go through the per-model text pricing in <see cref="OpenAIApiLogger"/>.
    ///
    /// The session is logged as one OpenAIApiLogs row whose ResponsePayload is <see cref="ToPayload"/>;
    /// the logger reprices that payload with <see cref="FromPayload"/> and <see cref="CostUSD"/>.
    /// </summary>
    public sealed class OpenAIRealtimeUsage
    {
        public int Responses { get; private set; }
        public long TextInputTokens { get; private set; }
        public long AudioInputTokens { get; private set; }
        public long CachedTextInputTokens { get; private set; }
        public long CachedAudioInputTokens { get; private set; }
        public long TextOutputTokens { get; private set; }
        public long AudioOutputTokens { get; private set; }

        public long InputTokens => TextInputTokens + AudioInputTokens;
        public long OutputTokens => TextOutputTokens + AudioOutputTokens;

        /// <summary>Adds the <c>response.usage</c> object of one <c>response.done</c> event.</summary>
        public void Add(JToken usage)
        {
            if (usage == null || usage.Type != JTokenType.Object)
                return;

            var input = usage["input_token_details"];
            var cached = input?["cached_tokens_details"];
            var output = usage["output_token_details"];

            Responses++;
            TextInputTokens += Long(input?["text_tokens"]);
            AudioInputTokens += Long(input?["audio_tokens"]);
            CachedTextInputTokens += Long(cached?["text_tokens"]);
            CachedAudioInputTokens += Long(cached?["audio_tokens"]);
            TextOutputTokens += Long(output?["text_tokens"]);
            AudioOutputTokens += Long(output?["audio_tokens"]);
        }

        /// <summary>
        /// The session in the Realtime API's own usage shape, so ExtractTokenUsage in the logger fills
        /// the prompt/completion token columns as for any other call.
        /// </summary>
        public string ToPayload() => JsonConvert.SerializeObject(new
        {
            realtimeSession = true,
            responses = Responses,
            usage = new
            {
                input_tokens = InputTokens,
                output_tokens = OutputTokens,
                total_tokens = InputTokens + OutputTokens,
                input_token_details = new
                {
                    text_tokens = TextInputTokens,
                    audio_tokens = AudioInputTokens,
                    cached_tokens = CachedTextInputTokens + CachedAudioInputTokens,
                    cached_tokens_details = new { text_tokens = CachedTextInputTokens, audio_tokens = CachedAudioInputTokens }
                },
                output_token_details = new { text_tokens = TextOutputTokens, audio_tokens = AudioOutputTokens }
            }
        });

        /// <summary>Reads a payload written by <see cref="ToPayload"/>; empty usage when it cannot be read.</summary>
        public static OpenAIRealtimeUsage FromPayload(string payload)
        {
            var usage = new OpenAIRealtimeUsage();
            try
            {
                if (!string.IsNullOrWhiteSpace(payload))
                {
                    var json = JObject.Parse(payload);
                    usage.Add(json["usage"]);
                    usage.Responses = json["responses"]?.Value<int?>() ?? usage.Responses;
                }
            }
            catch (JsonException)
            {
            }
            return usage;
        }

        /// <summary>Cost in US dollars at the rates for <paramref name="model"/>.</summary>
        public decimal CostUSD(string model)
        {
            var r = RatesFor(model);
            var uncachedText = Math.Max(0, TextInputTokens - CachedTextInputTokens);
            var uncachedAudio = Math.Max(0, AudioInputTokens - CachedAudioInputTokens);

            var cost = uncachedText * r.TextIn + CachedTextInputTokens * r.TextCached + TextOutputTokens * r.TextOut
                     + uncachedAudio * r.AudioIn + CachedAudioInputTokens * r.AudioCached + AudioOutputTokens * r.AudioOut;
            return Math.Round(cost / 1_000_000m, 6);
        }

        private static long Long(JToken token) =>
            token != null && token.Type == JTokenType.Integer ? token.Value<long>() : 0;

        /// <summary>USD per 1M tokens.</summary>
        internal readonly struct Rates
        {
            public Rates(decimal textIn, decimal textCached, decimal textOut, decimal audioIn, decimal audioCached, decimal audioOut)
            {
                TextIn = textIn; TextCached = textCached; TextOut = textOut;
                AudioIn = audioIn; AudioCached = audioCached; AudioOut = audioOut;
            }

            public decimal TextIn { get; }
            public decimal TextCached { get; }
            public decimal TextOut { get; }
            public decimal AudioIn { get; }
            public decimal AudioCached { get; }
            public decimal AudioOut { get; }
        }

        // From OpenAI's pricing page. The gpt-realtime audio rates were checked on 28 Oct 2026; the
        // preview and mini rates are OpenAI's published launch prices: check them against the invoice.
        private static readonly Rates GptRealtime = new Rates(4.00m, 0.40m, 16.00m, 32.00m, 0.40m, 64.00m);
        private static readonly Rates Gpt4oRealtimePreview = new Rates(5.00m, 2.50m, 20.00m, 40.00m, 2.50m, 80.00m);
        private static readonly Rates RealtimeMini = new Rates(0.60m, 0.30m, 2.40m, 10.00m, 0.30m, 20.00m);

        internal static Rates RatesFor(string model)
        {
            var m = (model ?? string.Empty).ToLowerInvariant();
            if (m.Contains("mini"))
                return RealtimeMini;
            if (m.Contains("gpt-4o-realtime"))
                return Gpt4oRealtimePreview;
            return GptRealtime;
        }
    }
}
