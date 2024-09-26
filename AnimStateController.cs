using EFT;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TarkovIRL
{
    internal class AnimStateController
    {
        public static readonly int _Blindfire1 = -1271366218;
        public static readonly int _Blindfire2 = 1276948056;
        public static readonly int _TurnState1 = -31136456;
        public static readonly int _TurnState2 = 287005718;

        static int _currentStateHash = 0;

        public static void SetCurrentState(int state)
        {
            _currentStateHash = state;
        }

        public static bool IsTurning
        {
            get { return _currentStateHash == _TurnState1 || _currentStateHash == _TurnState2; }
        }

        public static bool IsBlindfire
        {
            get { return _currentStateHash == _Blindfire1 || _currentStateHash == _Blindfire2; }
        }
    }
}
