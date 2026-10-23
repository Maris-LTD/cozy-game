using System.Collections.Generic;
using CozyGachapon.Contracts;

namespace CozyGachapon.Foundation
{
    public class InputBlockGate : IInputBlockGate
    {
        private readonly HashSet<int> _activeTokens = new HashSet<int>(32);
        private int _nextTokenId = 1;

        public bool IsInputBlocked => _activeTokens.Count > 0;
        public InputGateStatus Status => _activeTokens.Count > 0 ? InputGateStatus.Blocked : InputGateStatus.Open;
        public int ActiveTokenCount => _activeTokens.Count;

        public int AcquireInputBlock(string owner = null)
        {
            int token = _nextTokenId++;
            _activeTokens.Add(token);
            return token;
        }

        public void Release(int token)
        {
            if (token > 0)
            {
                _activeTokens.Remove(token);
            }
        }
    }
}
