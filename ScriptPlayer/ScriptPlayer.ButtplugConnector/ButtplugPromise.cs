using System;
using Buttplug.Core;

namespace ScriptPlayer.ButtplugConnector
{
    public abstract class ButtplugPromise
    {
        public abstract void SetResult(ButtplugMessage result);

        public void Cancel()
        {
            SetResult(null);
        }
    }

    public class ButtplugPromise<T> : ButtplugPromise where T : ButtplugMessage
    {
        private ButtplugMessage _result;

        private bool _resultIsSet;
        private bool _responseIsSet;
        private Action<T> _successFinal;
        private Action<ButtplugMessage> _failure;

        public override void SetResult(ButtplugMessage result)
        {
            if(_resultIsSet)
                throw new Exception("Result cannot be set more than once!");

            _result = result;
            _resultIsSet = true;

            TryExecute();
        }

        public void Then(Action<T> success, Action<ButtplugMessage> failure)
        {
            if (_responseIsSet)
                throw new Exception("Response cannot be set more than once!");

            _successFinal = success;
            _failure = failure;
            _responseIsSet = true;

            TryExecute();
        }

        private void TryExecute()
        {
            if (!_responseIsSet || !_resultIsSet)
                return;

            if (_result is T)
                _successFinal((T) _result);
            else
                _failure(_result);
        }
    }
}