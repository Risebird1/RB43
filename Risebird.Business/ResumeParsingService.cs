using Newtonsoft.Json;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Data.AffIndaModels;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Risebird.Business
{
    public class ResumeParsingService : IResumeParsingService
    {
        private readonly IApplicationErrorLogger _logger;
        private readonly ICandidateRepository _candidateRepository;
        private readonly IAzureStorageHelper _azureStorageHelper;
        private readonly IAffIndaResumeParserHelper _affIndaResumeParserHelper;
        private readonly IIRFService _irfService;
        private readonly IValidationHelper _validationHelper;
        private readonly IPdfReaderHelper _pdfReaderHelper;
        private readonly IAssistantServiceHelper _assistantServiceHelper;
        private readonly IResumeOptimizationHelper _resumeOptimizationHelper;

        public ResumeParsingService(
            IApplicationErrorLogger logger,
            ICandidateRepository candidateRepository,
            IAzureStorageHelper azureStorageHelper,
            IAffIndaResumeParserHelper affIndaResumeParserHelper,
            IIRFService irfService,
            IValidationHelper validationHelper,
            IPdfReaderHelper pdfReaderHelper,
            IAssistantServiceHelper assistantServiceHelper,
            IResumeOptimizationHelper resumeOptimizationHelper)
        {
            _logger = logger;
            _candidateRepository = candidateRepository;
            _azureStorageHelper = azureStorageHelper;
            _affIndaResumeParserHelper = affIndaResumeParserHelper;
            _irfService = irfService;
            _validationHelper = validationHelper;
            _pdfReaderHelper = pdfReaderHelper;
            _assistantServiceHelper = assistantServiceHelper;
            _resumeOptimizationHelper = resumeOptimizationHelper;
        }

        public async Task<ResumeParseResult> ParseResumeAsync(int candidateResumeId)
        {
            try
            {
                // Get the resume record directly by ID
                var resume = await _candidateRepository.GetCandidateResumeById(candidateResumeId);
                
                if (resume == null)
                {
                    return new ResumeParseResult
                    {
                        Success = false,
                        ErrorMessage = "Resume not found"
                    };
                }

                if (string.IsNullOrEmpty(resume.ResumeLink))
                {
                    await UpdateResumeStatus(resume.id, false, false, true, "Resume link is missing");
                    return new ResumeParseResult
                    {
                        Success = false,
                        ErrorMessage = "Resume link is missing"
                    };
                }

                CandidateResumeParsedDataOpenAI candidateParsedData = null;
                string resumeLinkToParse = resume.ResumeLink;

                // Convert to PDF if needed
                if (!resume.ResumeLink.Contains(".pdf"))
                {
                    try
                    {
                        resumeLinkToParse = await _azureStorageHelper.DownloadAndConvertFromDocToPdfAsync(resume.ResumeLink);
                    }
                    catch (Exception ex)
                    {
                        await _logger.LogMessage(LogLevelInfo.Exception, ex);
                        // Continue with original link
                    }
                }

                // Read PDF content
                var resumeStringContent = await _pdfReaderHelper.ReadPdfFromUrlAsync(resumeLinkToParse);

                if (!string.IsNullOrEmpty(resumeStringContent))
                {
                    // Validate resume content
                    var (isValid, validationReason) = _resumeOptimizationHelper.ValidateResumeForParsing(resumeStringContent);
                    if (!isValid)
                    {
                        await UpdateResumeStatus(resume.id, false, false, true, validationReason);
                        return new ResumeParseResult
                        {
                            Success = false,
                            ErrorMessage = $"Resume validation failed: {validationReason}"
                        };
                    }

                    // Optimize resume text
                    resumeStringContent = _resumeOptimizationHelper.OptimizeResumeText(resumeStringContent);

                    // Try OpenAI parsing first
                    try
                    {
                        var runResult = await _assistantServiceHelper.ParseResume(resumeStringContent);
                        var replyText = runResult?.Data?.FirstOrDefault()?.Content?.FirstOrDefault()?.Text?.Value;
                        candidateParsedData = JsonConvert.DeserializeObject<CandidateResumeParsedDataOpenAI>(replyText);
                    }
                    catch (Exception ex)
                    {
                        await _logger.LogMessage(LogLevelInfo.Exception, ex);
                        // OpenAI failed, will try AffInda below
                    }
                }

                // Process parsed data
                var candidateQuickAdd = new CandidateQuickAddModel
                {
                    ResumeLink = resume.ResumeLink,
                    CandidateResumeId = resume.id,
                    IRFId = resume.IRFId,
                    AddedByUserId = resume.AddedBy,
                    ClientHREmail = resume.ClientHREmailId
                };

                if (candidateParsedData != null)
                {
                    // Use OpenAI parsed data
                    candidateQuickAdd.Name = candidateParsedData.name;
                    
                    if (!string.IsNullOrEmpty(candidateParsedData.emailId))
                    {
                        candidateQuickAdd.EmailId = CleanEmail(candidateParsedData.emailId);
                        
                        if (!_validationHelper.ValidateEmailAddress(candidateQuickAdd.EmailId))
                        {
                            await UpdateResumeStatus(resume.id, true, false, false, "Parsed email is not valid", false);
                            return new ResumeParseResult
                            {
                                Success = false,
                                CandidateName = candidateQuickAdd.Name,
                                ErrorMessage = "Parsed email is not valid"
                            };
                        }
                    }
                    else
                    {
                        await UpdateResumeStatus(resume.id, true, false, false, "Email not found in resume", true);
                        return new ResumeParseResult
                        {
                            Success = false,
                            CandidateName = candidateQuickAdd.Name,
                            ErrorMessage = "Email not found in resume"
                        };
                    }

                    if (!string.IsNullOrEmpty(candidateParsedData.phoneNumber))
                    {
                        candidateQuickAdd.Phone = candidateParsedData.phoneNumber.Replace(" ", string.Empty);
                        if (candidateQuickAdd.Phone == "911" || candidateQuickAdd.Phone == "+911")
                        {
                            candidateQuickAdd.Phone = null;
                        }
                    }
                }
                else
                {
                    // Try AffInda as fallback
                    try
                    {
                        var affindaResult = await _affIndaResumeParserHelper.UploadAndParseResume(
                            resume.ResumeLink, 
                            resume.SkillChannelName, 
                            resume.IsAffindaIndexCreated);

                        if (!string.IsNullOrEmpty(affindaResult.Item1))
                        {
                            var documentData = JsonConvert.DeserializeObject<Document>(affindaResult.Item1);
                            candidateQuickAdd.Name = documentData.data?.name?.raw;

                            if (!string.IsNullOrEmpty(affindaResult.Item2))
                            {
                                candidateQuickAdd.AffindaResumeIdentifier = affindaResult.Item2;
                            }

                            if (documentData.data.emails != null && documentData.data.emails.Any())
                            {
                                candidateQuickAdd.EmailId = CleanEmail(documentData.data.emails[0]);

                                if (!_validationHelper.ValidateEmailAddress(candidateQuickAdd.EmailId))
                                {
                                    await UpdateResumeStatus(resume.id, true, false, false, "Parsed email is not valid", false);
                                    return new ResumeParseResult
                                    {
                                        Success = false,
                                        CandidateName = candidateQuickAdd.Name,
                                        ErrorMessage = "Parsed email is not valid"
                                    };
                                }
                            }
                            else
                            {
                                await UpdateResumeStatus(resume.id, true, false, false, "Email not found in resume", true);
                                return new ResumeParseResult
                                {
                                    Success = false,
                                    CandidateName = candidateQuickAdd.Name,
                                    ErrorMessage = "Email not found in resume"
                                };
                            }

                            if (documentData.data.phoneNumberDetails != null && documentData.data.phoneNumberDetails.Any())
                            {
                                candidateQuickAdd.Phone = documentData.data.phoneNumberDetails[0].rawText;
                                if (candidateQuickAdd.Phone == "911" || candidateQuickAdd.Phone == "+911")
                                {
                                    candidateQuickAdd.Phone = null;
                                }
                            }
                        }
                        else
                        {
                            await UpdateResumeStatus(resume.id, false, false, true, "Both OpenAI and AffInda parsing failed");
                            return new ResumeParseResult
                            {
                                Success = false,
                                ErrorMessage = "Resume parsing failed"
                            };
                        }
                    }
                    catch (Exception ex)
                    {
                        await _logger.LogMessage(LogLevelInfo.Exception, ex);
                        await UpdateResumeStatus(resume.id, false, false, true, $"AffInda parsing failed: {ex.Message}");
                        return new ResumeParseResult
                        {
                            Success = false,
                            ErrorMessage = $"Resume parsing failed: {ex.Message}"
                        };
                    }
                }

                // Add candidate to database
                var candidateList = new List<CandidateQuickAddModel> { candidateQuickAdd };
                await _irfService.AddCandidateUsingResumeParserJob(candidateList, resume.IRFId);

                // Get the created candidate info
                var candidateInfo = await _candidateRepository.GetCandidateByResumeId(candidateResumeId);

                return new ResumeParseResult
                {
                    Success = true,
                    CandidateName = candidateQuickAdd.Name,
                    CandidateEmail = candidateQuickAdd.EmailId,
                    CandidatePhone = candidateQuickAdd.Phone,
                    CandidateId = candidateInfo?.CandidateId,
                    CandidateMasterId = candidateInfo?.CandidateMasterId
                };
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return new ResumeParseResult
                {
                    Success = false,
                    ErrorMessage = $"Exception during parsing: {ex.Message}"
                };
            }
        }

        private string CleanEmail(string email)
        {
            if (string.IsNullOrEmpty(email)) return email;

            email = email.Trim()
                .Replace(":", "")
                .Replace("%20", "")
                .Replace("?:", "")
                .Replace(":%20", "")
                .Replace("-%20", "");

            if (email.StartsWith(":") || email.StartsWith("-"))
                email = email.Substring(1);
            if (email.StartsWith("%20"))
                email = email.Substring(3);
            if (email.StartsWith("?:"))
                email = email.Substring(2);
            if (email.StartsWith(":%20") || email.StartsWith("-%20"))
                email = email.Substring(4);

            return email;
        }

        private async Task UpdateResumeStatus(int resumeId, bool isParsed, bool isProcessed, bool parsingFailed, string reason, bool? isEmailMissing = null)
        {
            var resumeUpdate = new CandidateResume
            {
                id = resumeId,
                IsResumeParsed = isParsed,
                IsProcessed = isProcessed,
                IsResumeParsingFailed = parsingFailed,
                ReasonBehindNotProcessed = reason
            };

            if (isEmailMissing.HasValue)
            {
                resumeUpdate.IsEmailMissing = isEmailMissing.Value;
            }

            await _candidateRepository.UpdateCandidateResumeInBulk(new List<CandidateResume> { resumeUpdate });
        }
    }
}
