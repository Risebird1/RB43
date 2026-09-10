using System.Collections;
using System.Net.Http;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Moq;

namespace Risebird.Business.Tests.Infrastructure
{
    /// <summary>
    /// Constructs any Risebird business service from its constructor signature, substituting a
    /// Moq double for each injected interface, an in-memory <see cref="IConfiguration"/> for
    /// configuration, and an offline <see cref="HttpClient"/> for HTTP.
    ///
    /// Every mock is created with <see cref="DefaultValueProvider.Mock"/> so that Task-returning
    /// members hand back completed tasks and collection-returning members hand back empty
    /// collections — a service under test therefore walks its happy path instead of
    /// null-referencing on the first await.
    /// </summary>
    internal sealed class AutoMocker
    {
        private readonly Dictionary<Type, Mock> _mocks = new();
        private readonly StubHttpMessageHandler _httpHandler;

        /// <summary>Keys seeded so configuration-reading services find plausible values.</summary>
        private static readonly Dictionary<string, string> DefaultSettings = new()
        {
            ["ProctoringApiSettings:ApiBaseUrl"] = "https://proctor.risebird.test",
            ["ProctoringApiSettings:DefaultTokenTtlMinutes"] = "180",
            ["AzureSettings:endPoint"] = "https://management.azure.test/subscriptions/{0}/",
            ["AzureSettings:ApiVersionLocation"] = "2022-01-01",
            ["AzureSettings:ApiVersionVMList"] = "2022-01-01",
            ["AzureSettings:ApiVersionDeleteVM"] = "2022-01-01",
            ["AzureSettings:ApiVersionDeleteResourceGroup"] = "2022-01-01",
            ["AzureSettings:templatelocation"] = "https://blob.risebird.test/template.json",
            ["AzureSearch:Endpoint"] = "https://search.risebird.test",
            ["AzureSearch:ApiKey"] = "test-key",
            ["EncryptionDetails:Key"] = "ThisIsA32ByteLongKeyForAES256!!!",
            ["EncryptionDetails:GenericKey"] = "GenericKey32BytesLongForAES256!!",
            ["EncryptionDetails:BaseEncodedKey"] = "BaseEncodedKey32BytesLongAES256!",
            ["CodeExecutionSettings:BaseUrl"] = "https://code.risebird.test/",
            ["CodeExecutionSettings:ApiKey"] = "test-key",
            ["ApplicationSettings:SiteUrl"] = "https://app.risebird.test/",
            ["ApplicationSettings:BaseUrl"] = "https://app.risebird.test/",
            ["OpenAI:ApiKey"] = "test-key",
            ["OpenAI:Model"] = "gpt-test",

            // Below: every remaining key Risebird.Business actually reads. A service that gates
            // itself on configuration ("no BaseUrl, so report the runner as unavailable") returns
            // at its first line when the key is missing, and none of its real work is reachable.
            // CodeExecutionService reads the "CodeExecution" section — the "CodeExecutionSettings"
            // pair above is a different, unused prefix.
            ["CodeExecution:Enabled"] = "true",
            ["CodeExecution:Provider"] = "Piston",
            ["CodeExecution:BaseUrl"] = "https://piston.risebird.test",
            ["CodeExecution:ApiKey"] = "test-key",
            ["CodeExecution:TimeoutSeconds"] = "25",
            ["CodeExecution:MaxOutputCharacters"] = "20000",

            ["Cashfree:BaseUrl"] = "https://cashfree.risebird.test",
            ["Cashfree:ClientId"] = "test-client",
            ["Cashfree:ClientSecret"] = "test-secret",
            ["Cashfree:ApiVersion"] = "2024-01-01",
            ["Cashfree:TransferMode"] = "banktransfer",
            ["Cashfree:VerificationBaseUrl"] = "https://verify.risebird.test",
            ["Cashfree:VerificationApiVersion"] = "2024-01-01",

            ["HostDetails:baseUrl"] = "https://app.risebird.test/",
            ["Plivo:FromNumber"] = "+919876543210",
            ["ProctoringApiSettings:Policy"] = "default",

            ["OpenAI:QcInterviewAIMergeAssistantId"] = "asst_test",
            ["OpenAI:SetCreationPromptId"] = "pmpt_test",
            ["OpenAI:SetCreationPromptVersion"] = "1",

            ["Vapi:InterviewBookingAssistantId"] = "asst_test",
            ["Vapi:PhoneNumberId"] = "phone_test",

            ["TwilioSettings:ACCOUNT_SID"] = "ACtest",
            ["TwilioSettings:AUTH_TOKEN"] = "test-token",
            ["TwilioSettings:CallerNumber"] = "+919876543210",

            ["RisebirdDetails:AccountName"] = "Risebird Test",
            ["RisebirdDetails:AccountNumber"] = "000123456789",
            ["RisebirdDetails:Address"] = "1 Test Street, Bengaluru",
            ["RisebirdDetails:BankName"] = "Test Bank",
            ["RisebirdDetails:CIN"] = "U72900KA2020PTC000000",
            ["RisebirdDetails:GSTIN"] = "29AAAAA0000A1Z5",
            ["RisebirdDetails:ICICIBankSWIFTCode"] = "ICICINBBTST",
            ["RisebirdDetails:IFSC"] = "ICIC0000001",
            ["RisebirdDetails:PAN"] = "AAAAA0000A",
            ["RisebirdDetails:TAN"] = "BLRT00000A",
        };

        private readonly DefaultValueProvider _valueProvider;

        public AutoMocker(
            IReadOnlyDictionary<string, string> extraSettings = null,
            DefaultValueProvider valueProvider = null,
            System.Net.HttpStatusCode httpStatus = System.Net.HttpStatusCode.OK)
        {
            // Default stays DefaultValueProvider.Mock so every existing suite behaves exactly as
            // before; the sweeps pass a provider to steer collaborators down a different path.
            _valueProvider = valueProvider ?? DefaultValueProvider.Mock;

            var settings = new Dictionary<string, string>(DefaultSettings);
            if (extraSettings != null)
                foreach (var kv in extraSettings)
                    settings[kv.Key] = kv.Value;

            Configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
            _httpHandler = new StubHttpMessageHandler(httpStatus);
            HttpClient = new HttpClient(_httpHandler) { BaseAddress = new Uri("https://stub.risebird.test/") };
        }

        public IConfiguration Configuration { get; }

        public HttpClient HttpClient { get; }

        public IReadOnlyList<HttpRequestMessage> HttpRequests => _httpHandler.Requests;

        /// <summary>The mock standing in for <paramref name="serviceType"/>, created on first use.</summary>
        public Mock MockFor(Type serviceType)
        {
            if (_mocks.TryGetValue(serviceType, out var existing))
                return existing;

            var mockType = typeof(Mock<>).MakeGenericType(serviceType);
            var mock = (Mock)Activator.CreateInstance(mockType);
            mock.DefaultValueProvider = _valueProvider;
            _mocks[serviceType] = mock;
            return mock;
        }

        public Mock<T> MockFor<T>() where T : class => (Mock<T>)MockFor(typeof(T));

        /// <summary>Instantiates <paramref name="serviceType"/> using its greediest public constructor.</summary>
        public object CreateService(Type serviceType)
        {
            if (serviceType == null)
                throw new ArgumentNullException(nameof(serviceType));

            var constructor = serviceType
                .GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                .OrderByDescending(c => c.GetParameters().Length)
                .FirstOrDefault();

            if (constructor == null)
                throw new InvalidOperationException($"{serviceType.Name} has no public instance constructor.");

            var parameters = constructor.GetParameters();
            var args = new object[parameters.Length];
            for (var i = 0; i < parameters.Length; i++)
                args[i] = ResolveParameter(parameters[i]);

            return constructor.Invoke(args);
        }

        public T CreateService<T>() => (T)CreateService(typeof(T));

        private object ResolveParameter(ParameterInfo parameter)
        {
            var type = parameter.ParameterType;

            // An optional array/nullable parameter's own default is the most faithful choice —
            // it is what production DI passes, so the service keeps its configured behaviour
            // (e.g. the built-in reminder offsets rather than an empty schedule).
            if (parameter.HasDefaultValue && (type.IsArray || Nullable.GetUnderlyingType(type) != null))
                return parameter.DefaultValue;

            return Resolve(type);
        }

        /// <summary>Best-effort value for an arbitrary dependency type.</summary>
        public object Resolve(Type type)
        {
            if (type == typeof(IConfiguration))
                return Configuration;

            if (type == typeof(HttpClient))
                return HttpClient;

            if (type == typeof(string))
                return "test";

            if (type == typeof(HttpMessageHandler))
                return _httpHandler;

            if (type.IsInterface)
            {
                // A closed generic collection interface is data, not a collaborator.
                if (typeof(IEnumerable).IsAssignableFrom(type) && type != typeof(IEnumerable))
                    return CreateEmptyCollection(type);

                return MockFor(type).Object;
            }

            if (type.IsArray)
                return Array.CreateInstance(type.GetElementType(), 0);

            if (Nullable.GetUnderlyingType(type) != null)
                return null;

            if (type.IsValueType)
                return Activator.CreateInstance(type);

            if (type.IsAbstract)
                return CanBeMocked(type) ? MockFor(type).Object : null;

            // A concrete collaborator: prefer a Moq subclass so calls are inert, then fall back
            // to real construction, then to null.
            if (CanBeMocked(type))
            {
                try { return MockFor(type).Object; }
                catch { /* non-virtual members or no accessible ctor — fall through */ }
            }

            try { return CreateService(type); }
            catch { return null; }
        }

        private static bool CanBeMocked(Type type) =>
            !type.IsSealed
            && type.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                   .Any(c => c.GetParameters().Length == 0);

        private static object CreateEmptyCollection(Type interfaceType)
        {
            if (!interfaceType.IsGenericType)
                return Array.Empty<object>();

            var elementType = interfaceType.GetGenericArguments()[0];
            var listType = typeof(List<>).MakeGenericType(elementType);
            return Activator.CreateInstance(listType);
        }
    }
}
