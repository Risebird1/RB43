using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace WebApplication1.Models
{
    public class BasicAuth : AuthorizationFilterAttribute
    {
        public override void OnAuthorization(HttpActionContext actionContext)
        {
            if (actionContext.Request.Headers.Authorization == null)
            {
                actionContext.Response = actionContext.Request.CreateResponse(HttpStatusCode.Unauthorized);
            }
            else
            {
                string Authnticationtoken = actionContext.Request.Headers.Authorization.Parameter;
                string details = Encoding.UTF8.GetString(Convert.FromBase64String(Authnticationtoken));
                string UserName = details.Split(':')[0];
                string Password = details.Split(':')[1];
                if (UserName == "FaceApi" && Password == "FaceApi")
                {

                }
                else
                {
                    actionContext.Response = actionContext.Request.CreateResponse(HttpStatusCode.Unauthorized);
                }

            }
        }

    }
}