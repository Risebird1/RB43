# Service Registration Guide for Vapi Integration

## 📍 Where to Add Services

Open `Risebird.Web/Program.cs` and locate line 217 (just before `var app = builder.Build();`)

---

## 🔧 Services to Add

### Add these lines after line 217 in Program.cs:

```csharp
// ============================================
// Vapi.ai Voice AI Integration Services
// ============================================
builder.Services.AddHttpClient(); // Required for HTTP calls (add only if not already present)
builder.Services.AddScoped<IVapiService, VapiService>();
builder.Services.AddScoped<IVapiCallRepository, VapiCallRepository>();
builder.Services.AddScoped<IVapiInterviewService, VapiInterviewService>();
// ============================================
```

---

## 📄 Complete Program.cs Section (Lines 210-225)

### Before (Original):

```csharp
builder.Services.AddScoped<IAIInterviewerRepository, AIInterviewerRepository>();
builder.Services.AddScoped<IOpenAIInterviewerAssistantHelper, OpenAIInterviewerAssistantHelper>();
builder.Services.AddScoped<IOpenAISpeechToTextService, OpenAISpeechToTextService>();
builder.Services.AddScoped<IAIInterviewerService, AIInterviewerService>();
builder.Services.AddScoped<ISynthesiaTextToVideoService, SynthesiaTextToVideoService>();
builder.Services.AddScoped<IZohoMeetingHelper, ZohoMeetingHelper>();
builder.Services.AddScoped<IAWSRekognitionService, AWSRekognitionService>();
builder.Services.AddDefaultAWSOptions(builder.Configuration.GetAWSOptions());

var app = builder.Build();
```

### After (With Vapi Services):

```csharp
builder.Services.AddScoped<IAIInterviewerRepository, AIInterviewerRepository>();
builder.Services.AddScoped<IOpenAIInterviewerAssistantHelper, OpenAIInterviewerAssistantHelper>();
builder.Services.AddScoped<IOpenAISpeechToTextService, OpenAISpeechToTextService>();
builder.Services.AddScoped<IAIInterviewerService, AIInterviewerService>();
builder.Services.AddScoped<ISynthesiaTextToVideoService, SynthesiaTextToVideoService>();
builder.Services.AddScoped<IZohoMeetingHelper, ZohoMeetingHelper>();
builder.Services.AddScoped<IAWSRekognitionService, AWSRekognitionService>();
builder.Services.AddDefaultAWSOptions(builder.Configuration.GetAWSOptions());

// ============================================
// ✨ NEW: Vapi.ai Voice AI Integration
// ============================================
builder.Services.AddHttpClient(); // For HTTP API calls
builder.Services.AddScoped<IVapiService, VapiService>();
builder.Services.AddScoped<IVapiCallRepository, VapiCallRepository>();
builder.Services.AddScoped<IVapiInterviewService, VapiInterviewService>();
// ============================================

var app = builder.Build();
```

---

## 🎯 Step-by-Step Instructions

### Step 1: Open Program.cs
```
File Location: Risebird.Web\Program.cs
```

### Step 2: Find the Service Registration Section
Search for (Ctrl+F):
```csharp
builder.Services.AddScoped<IAWSRekognitionService, AWSRekognitionService>();
```

### Step 3: Add the New Services
After the AWS line and before `var app = builder.Build();`, add:

```csharp
// Vapi.ai Voice AI Integration Services
builder.Services.AddHttpClient();
builder.Services.AddScoped<IVapiService, VapiService>();
builder.Services.AddScoped<IVapiCallRepository, VapiCallRepository>();
builder.Services.AddScoped<IVapiInterviewService, VapiInterviewService>();
```

### Step 4: Save File
Press Ctrl+S

### Step 5: Build Solution
```bash
# In Visual Studio: Ctrl+Shift+B
# Or via terminal:
dotnet build
```

---

## ✅ Verification

### After Adding Services:

1. **No Build Errors**: Solution should build successfully
2. **Dependency Injection Working**: Services will be available in controllers
3. **Ready to Use**: Can now inject services in controllers:

```csharp
public class YourController : Controller
{
    private readonly IVapiService _vapiService;
    private readonly IVapiCallRepository _vapiCallRepository;
    
    public YourController(
        IVapiService vapiService,
        IVapiCallRepository vapiCallRepository)
    {
        _vapiService = vapiService;
        _vapiCallRepository = vapiCallRepository;
    }
    
    // Now you can use the services!
}
```

---

## 🚨 Common Issues

### Issue: "IVapiService not found"
**Cause**: Using statement missing  
**Fix**: Add to top of file:
```csharp
using Risebird.CommonHelper.Interface;
```

### Issue: "VapiService not found"
**Cause**: Using statement missing  
**Fix**: Add to top of file:
```csharp
using Risebird.CommonHelper;
```

### Issue: "The type or namespace name 'IVapiCallRepository' could not be found"
**Cause**: Using statement missing  
**Fix**: Add to top of file:
```csharp
using Risebird.Repository.Interface;
using Risebird.Repository;
```

### Issue: "Build errors after adding services"
**Cause**: Project references missing  
**Fix**: 
1. Verify all projects are in solution
2. Check project dependencies:
   - Web → Business → Repository → Entities
   - Web → CommonHelper → Entities
3. Rebuild solution: `dotnet build`

---

## 🧪 Test Service Registration

### Create a test controller:

```csharp
// Risebird.Web/Controllers/VapiTestController.cs
using Microsoft.AspNetCore.Mvc;
using Risebird.CommonHelper.Interface;
using Risebird.Repository.Interface;
using System.Threading.Tasks;

namespace Risebird.Web.Controllers
{
    public class VapiTestController : Controller
    {
        private readonly IVapiService _vapiService;
        private readonly IVapiCallRepository _vapiCallRepository;
        
        public VapiTestController(
            IVapiService vapiService,
            IVapiCallRepository vapiCallRepository)
        {
            _vapiService = vapiService;
            _vapiCallRepository = vapiCallRepository;
        }
        
        [HttpGet]
        public async Task<IActionResult> TestServices()
        {
            try
            {
                // Test 1: Service injection working
                var serviceTest = _vapiService != null && _vapiCallRepository != null;
                
                // Test 2: Can call API
                var assistants = await _vapiService.ListAssistantsAsync();
                
                // Test 3: Can access database
                var recentCalls = await _vapiCallRepository.GetRecentCallLogsAsync(1);
                
                return Json(new
                {
                    success = true,
                    message = "All services working correctly!",
                    details = new
                    {
                        servicesInjected = serviceTest,
                        apiAccessible = !string.IsNullOrEmpty(assistants),
                        databaseAccessible = recentCalls != null
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Service registration issue",
                    error = ex.Message
                });
            }
        }
    }
}
```

### Test the services:
1. Build solution
2. Run application
3. Navigate to: `https://localhost:44360/VapiTest/TestServices`
4. Should see:
```json
{
  "success": true,
  "message": "All services working correctly!",
  "details": {
    "servicesInjected": true,
    "apiAccessible": true,
    "databaseAccessible": true
  }
}
```

---

## 📋 Complete Checklist

- [ ] Opened Program.cs
- [ ] Found service registration section (line ~217)
- [ ] Added HttpClient service
- [ ] Added IVapiService / VapiService
- [ ] Added IVapiCallRepository / VapiCallRepository  
- [ ] Added IVapiInterviewService / VapiInterviewService
- [ ] Saved file
- [ ] Built solution successfully (no errors)
- [ ] Tested service injection (optional)

---

## 🎉 Done!

Your Vapi services are now registered and ready to use throughout your application!

**Next Steps:**
1. ✅ Services registered (You are here)
2. ⏭️ Configure appsettings.json (see `VapiConfigurationSnippet.json`)
3. ⏭️ Create database table (run `VapiCallLogs.sql`)
4. ⏭️ Setup assistant (via `/VapiInterviewCall/SetupAssistant`)
5. ⏭️ Make test call!

**Need help?** See `IMPLEMENTATION_GUIDE.md`
