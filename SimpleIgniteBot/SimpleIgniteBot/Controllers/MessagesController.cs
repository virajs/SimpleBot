﻿using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using EventBot.SupportLibrary.Services;
using Microsoft.ApplicationInsights;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using SimpleIgniteBot.Bot;
using LuisModel = SimpleIgniteBot.LUIS.LuisModel;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace SimpleIgniteBot.Controllers
{
    [System.Web.Mvc.Route("api/[controller]")]
    [BotAuthentication]
    public class MessagesController : ApiController
    {
        private string Intro = "Hi there. Welcome to the bot demo";
        public MessagesController()
        {

        }

        [System.Web.Mvc.Route("")]
        [System.Web.Mvc.HttpGet]
        public async Task<HttpResponseMessage> Get()
        {
            var response = Request.CreateResponse(HttpStatusCode.OK);
            return response;
        }

        [System.Web.Mvc.Route("")]
        [System.Web.Mvc.HttpPost]
        public async Task<HttpResponseMessage> Post([FromBody] Activity activity)
        {
            var translator = new TranslatorService();
            if (activity == null)
            {
                //tc.TrackTrace("MessagesController::NULL Activity received");
            }
            else if (activity.Type == ActivityTypes.Message)
            {
                var text = activity.Text;

                if (text.ToLowerInvariant() == "ping")
                {
                    ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));
                    Activity reply = activity.CreateReply("pong");
                    await connector.Conversations.ReplyToActivityAsync(reply);
                }
                else if (activity.Text.ToLowerInvariant().Contains("command language"))
                {
                    var t = activity.Text.ToLowerInvariant().Replace("command language", "");
                    t = t.Trim();
                    
                    await translator.SetLanguage(activity, t);

                    ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));
                    Activity reply = activity.CreateReply(await translator.TranslateBack(activity, "Your language preference has been set!"));
                    await connector.Conversations.ReplyToActivityAsync(reply);
                }
                else
                {
                    var language = await translator.GetLanguage(activity);

                    if (!string.IsNullOrWhiteSpace(language))
                    {
                        var detected = await translator.Detect(text);

                        if (detected == "en")
                        {
                            //do nothing becasue it might be a response. 
                            //  await translator.SetLanguage(activity, null);
                        }
                        else
                        {
                            var translated = await translator.Translate(text, language);
                            if (!string.IsNullOrWhiteSpace(translated))
                            {
                                activity.Text = translated;
                            }
                        }

                    }
                    await Conversation.SendAsync(activity, () => new LuisModel());
                }
            }
            else
            {
                await _handleSystemMessage(activity);
            }
            var response = Request.CreateResponse(HttpStatusCode.OK);
            return response;
        }

        private async Task<Activity> _handleSystemMessage(Activity message)
        {
            if (message.Type == ActivityTypes.DeleteUserData)
            {
                // Implement user deletion here
                // If we handle user deletion, return a real message
            }
            else if (message.Type == ActivityTypes.ConversationUpdate)
            {
                TelemetryClient tc = new TelemetryClient();
                tc.TrackTrace($"HandleSystemMessage::ActivityTypes.ConversationUpdate - {message.Action}");
                // Handle conversation state changes, like members being added and removed
                // Use Activity.MembersAdded and Activity.MembersRemoved and Activity.Action for info
                // Not available in all channels
                Activity.CreateConversationUpdateActivity();
                ConnectorClient client = new ConnectorClient(new Uri(message.ServiceUrl));

                var reply = message.CreateReply();
                reply.Text = this.Intro;
                await client.Conversations.ReplyToActivityAsync(reply);
            }
            else if (message.Type == ActivityTypes.ContactRelationUpdate)
            {
                TelemetryClient tc = new TelemetryClient();
                tc.TrackTrace($"HandleSystemMessage::ActivityTypes.ConversationUpdate - {message.Action}");

                Activity.CreateContactRelationUpdateActivity();
                ConnectorClient client = new ConnectorClient(new Uri(message.ServiceUrl));

                var reply = message.CreateReply();
                reply.Text = this.Intro;
                await client.Conversations.ReplyToActivityAsync(reply);
            }
            else if (message.Type == ActivityTypes.Typing)
            {
                // Handle knowing tha the user is typing
            }
            else if (message.Type == ActivityTypes.Ping)
            {
            }


            return null;
        }
    }
}
