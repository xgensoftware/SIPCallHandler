using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
        TextToSpeech _txtToSpeech;
        WaveStreamPlayback _wavPlayer;

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

        void StopContactIdConnector()
        {
            _phoneCallAudioReceiver.Detach();
            _phoneCallAudioSender.Detach();

            _mediaConnector.Disconnect(_contactIdHandler, _phoneCallAudioSender);
            _mediaConnector.Disconnect(_phoneCallAudioReceiver, _contactIdHandler);

            _contactIdHandler.Stop();

            OnMessageEvent?.Invoke("Contact id handler stopped");
        }

        void StartTextToSpeech(string message)
        {
            _txtToSpeech = new TextToSpeech();

            Thread.Sleep(3000);
             _phoneCallAudioSender.AttachToCall(_call);
            _mediaConnector.Connect(_txtToSpeech, _phoneCallAudioSender);

            _txtToSpeech.AddAndStartText(message);

            OnMessageEvent?.Invoke($"Sending text to speech: {message}");
        }

        void StopSpeachConnector()
        {
            _phoneCallAudioSender.Detach();
            _mediaConnector.Disconnect(_txtToSpeech, _phoneCallAudioSender);
            _txtToSpeech.Stop();

            OnMessageEvent?.Invoke("Text To Speach stopped");
            _txtToSpeech = null;
        }

        void SendDtmfTone(DtmfNamedEvents tone)
        {
            Thread.Sleep(1000);
            _call.StartDTMFSignal(tone);
            Thread.Sleep(80);
            _call.StopDTMFSignal(tone);
        }

        void SetupWavPlayer(string pathToFile)
        {
            _wavPlayer = new WaveStreamPlayback(pathToFile);

            Thread.Sleep(3000);

            _mediaConnector.Connect(_wavPlayer, _phoneCallAudioSender);
            _phoneCallAudioSender.AttachToCall(_call);

            _wavPlayer.Start();

            OnMessageEvent?.Invoke("Playing wave file");
        }

        void StopWavPlayer()
        {
            _phoneCallAudioSender.Detach();
            _mediaConnector.Disconnect(_wavPlayer, _phoneCallAudioSender);
            _wavPlayer.Stop();
            OnMessageEvent?.Invoke("Stopping wave player");
        }

        void StartRecording()
        {

        }

        private void _softPhone_IncomingCall(object sender, Ozeki.Media.VoIPEventArgs<IPhoneCall> e)
        {
            OnMessageEvent?.Invoke($"Incoming call from {e.Item.DialInfo.ToString()}. Caller Id: {e.Item.DialInfo.CallerID}");

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
                //OnMessageEvent?.Invoke($"Codec: {s.CodecName}:{s.CodecType}, Payload Type: {s.PayloadType.ToString()}");
                _softPhone.DisableCodec(s.PayloadType);
            }
            _softPhone.EnableCodec(0);

            var registrationRequired = true;
            var userName = "17778668026";
            var displayName = "OzekiServicee";
            var authenticationId = "17778668026";
            var registerPassword = "Mark9441";
            var domainHost = "callcentric.com";
            var domainPort = 5060;

            var account = new SIPAccount(registrationRequired, displayName, userName, authenticationId, registerPassword, domainHost, domainPort);

            // Send SIP regitration request
            RegisterAccount(account);

            _mediaConnector = new MediaConnector();
            _phoneCallAudioSender = new PhoneCallAudioSender();
            _phoneCallAudioReceiver = new PhoneCallAudioReceiver();
            _contactIdHandler = new ContactIdHandler();
            
            
            _contactIdHandler.ContactIdSendFailed += _contactIdHandler_ContactIdSendFailed;
            _contactIdHandler.ContactIdSendSuccessful += _contactIdHandler_ContactIdSendSuccessful;
            _contactIdHandler.ReceivedNotification += _contactIdHandler_ReceivedNotification;
          
        }

        private void _contactIdHandler_ContactIdSendSuccessful(object sender, ContactIDSendEventArgs e)
        {
            OnMessageEvent?.Invoke($"Contact id data sent {e.Message}");
        }

        private void _contactIdHandler_ContactIdSendFailed(object sender, ContactIDSendEventArgs e)
        {
            OnMessageEvent?.Invoke($"Failed to send contact Id {e.Message}");
        }

        private void _contactIdHandler_ReceivedNotification(object sender, ContactIdNotificationEventArg e)
        {
            OnMessageEvent?.Invoke($"Received:Account: {e.AccountNumber} Event: {e.EventCode} Zone: {e.ZoneNumber}");
            if(e.EventCode == "606")
            {
                Thread.Sleep(2000);

                StopContactIdConnector();

                //2. repeat unit id back
                StartTextToSpeech($"The unit id for the current device is {e.AccountNumber}");
                StopSpeachConnector();

                //3. Lookup account number in salesforce?
                // if the account is found with active status goto step 6 else goto 4

                //4.Record message from caller
                
                //5. Playback their message then goto 7


                //6. Play text to speech letting the caller know the account is in use

                //7. hangup
            }
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
