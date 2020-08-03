using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;

namespace Risebird.Business
{
    public class ChatService : IChatService
    {
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly INotificationHelper _notificationHelper;
        private readonly IChatRepository _chatRepository;
        public ChatService(INotificationHelper notificationHelper, IApplicationErrorLogger applicationErrorLogger, IChatRepository chatRepository)
        {
            _applicationErrorLogger = applicationErrorLogger;
            _notificationHelper = notificationHelper;
            _chatRepository = chatRepository;
        }
        public List<Conversation> GetConversation(string userid, string contact, string status, string WhoSend)
        {
            try
            {
                return _chatRepository.GetConversation(userid, contact, status, WhoSend);
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public void AddConversation(string userid, string contact, string message)
        {
            try
            {
                _chatRepository.AddConversation(userid, contact, message);
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }
        public void AddGroup(string GroupName)
        {
            try
            {
                _chatRepository.AddGroup(GroupName);
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }
        public List<GroupDetails> FetchGroup()
        {
            try
            {
                return _chatRepository.FetchGroup();
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public void DeleteGroup(string GroupId)
        {
            try
            {
                _chatRepository.DeleteGroup(GroupId);
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }
        public List<GroupDetails> FetchUser(string GroupId)
        {
            try
            {
                return _chatRepository.FetchUser(GroupId);
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public string AddUser(string GroupId, string Emailid)
        {
            try
            {
                return _chatRepository.AddUser(GroupId, Emailid);
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return "";
            }
        }
        public void DeleteUser(string Userid)
        {
            try
            {
                _chatRepository.DeleteUser(Userid);
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }

        }
        public string Savetheme(string theme, string userid)
        {
            try
            {
                return _chatRepository.Savetheme(theme, userid);
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return "";
            }
        }



    }
}
