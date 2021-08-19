using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ozeki.VoIP;
using Ozeki.Media;
using com.Xgensoftware.Core;
namespace SIPCallHandler
{
    public class OzekiService
    {
        public delegate void MessageHandlerArg(string message);
        public event MessageHandlerArg OnMessageEvent;

        #region Member Variables
        ISoftPhone _softPhone = null;
        IPhoneLine _phoneLine = null;
        IPhoneCall _call = null;

        PhoneCallAudioSender _phoneCallAudioSender;
        PhoneCallAudioReceiver _phoneCallAudioReceiver;

        MediaConnector _mediaConnector;
        ContactIdHandler _contactIdHandler;
        #endregion

        #region Private Methods
        void RegisterAccount(SIPAccount account)
        {
            try
            {
                _phoneLine = _softPhone.CreatePhoneLine(account);
                _softPhone.IncomingCall += _softPhone_IncomingCall;
                _phoneLine.RegistrationStateChanged += _phoneLine_RegistrationStateChanged;
                _softPhone.RegisterPhoneLine(_phoneLine);
            }
            catch(Exception e)
            {
                OnMessageEvent?.Invoke($"Failed to register phone line. ERROR: {e.Message}");
            }
        }

        void SetupDevices()
        {
            _phoneCallAudioReceiver.AttachToCall(_call);
            _phoneCallAudioSender.AttachToCall(_call);

            _mediaConnector.Connect(_contactIdHandler, _phoneCallAudioSender);
            _mediaConnector.Connect(_phoneCallAudioReceiver, _contactIdHandler);

            _contactIdHandler.Start();

            OnMessageEvent?.Invoke("Contact id handler started");
        }

        void StopDevices()
        {
            _phoneCallAudioReceiver.Detach();
            _phoneCallAudioSender.Detach();

            _mediaConnector.Disconnect(_contactIdHandler, _phoneCallAudioSender);
            _mediaConnector.Disconnect(_phoneCallAudioReceiver, _contactIdHandler);

            _contactIdHandler.Stop();

            OnMessageEvent?.Invoke("Contact id handler stopped");
        }

        private void _softPhone_IncomingCall(object sender, Ozeki.Media.VoIPEventArgs<IPhoneCall> e)
        {
            OnMessageEvent?.Invoke($"Incoming call from {e.Item.DialInfo.ToString()}");

            _call = e.Item;
            _call.CallStateChanged += _call_CallStateChanged;
            _call.Answer();
        }

        private void _call_CallStateChanged(object sender, CallStateChangedArgs e)
        {
           
            OnMessageEvent?.Invoke($"Call state: {e.State.ToString()}:{e.Reason}");

            if(e.State == CallState.Answered)
            {
                SetupDevices();
            }
            else if(e.State == CallState.Completed)
            {
                StopDevices();

                OnMessageEvent?.Invoke($"Call {_call.DialInfo.CallerID} ended.");
            }
        }

        private void _phoneLine_RegistrationStateChanged(object sender, RegistrationStateChangedArgs e)
        {
            if (e.State == RegState.Error || e.State == RegState.NotRegistered)
                OnMessageEvent?.Invoke($"Registration Failed - {e.State}");

            if (e.State == RegState.RegistrationSucceeded)
                OnMessageEvent?.Invoke("Registration succeeded - Online!");
        }
        #endregion

        #region Public Methods
        public void Start()
        {
            OnMessageEvent?.Invoke("OzekiService starting....");
            
            _softPhone = SoftPhoneFactory.CreateSoftPhone(5000, 10000);

            foreach (var s in _softPhone.Codecs)
            {
                OnMessageEvent?.Invoke($"Codec: {s.CodecName}:{s.CodecType}, Payload Type: {s.PayloadType.ToString()}");
                _softPhone.DisableCodec(s.PayloadType);
            }
            _softPhone.EnableCodec(8);

            var registrationRequired = true;
            var userName = "17778668026";
            var displayName = "OzekiServicee";
            var authenticationId = "17778668026";
            var registerPassword = "TestingApp1";
            var domainHost = "callcentric.com";
            var domainPort = 5060;

            var account = new SIPAccount(registrationRequired, displayName, userName, authenticationId, registerPassword, domainHost, domainPort);

            // Send SIP regitration request
            RegisterAccount(account);

            _mediaConnector = new MediaConnector();
            _phoneCallAudioSender = new PhoneCallAudioSender();
            _phoneCallAudioReceiver = new PhoneCallAudioReceiver();
            _contactIdHandler = new ContactIdHandler();

            _contactIdHandler.ReceivedNotification += _contactIdHandler_ReceivedNotification;

        }

        private void _contactIdHandler_ReceivedNotification(object sender, ContactIdNotificationEventArg e)
        {
            OnMessageEvent?.Invoke($"Received data: {e.ToString()}");
        }

        public void Stop()
        {
            if(_softPhone != null)
            {
                _softPhone.Close();
            }

            OnMessageEvent?.Invoke("OzekiService stopping....");
        }
        #endregion
    }
}
