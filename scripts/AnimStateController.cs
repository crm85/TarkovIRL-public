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
        public enum EWeaponState { INTO_RELOAD, MID_RELOAD, OUT_OF_RELOAD, IDLE, CHECK_MAG };
        static EWeaponState _weaponState;

        // body states
        public static readonly int _Blindfire1 = -1271366218;
        public static readonly int _Blindfire2 = 1276948056;
        public static readonly int _TurnState1 = -31136456;
        public static readonly int _TurnState2 = 287005718;
        public static readonly int _LeftShoulderStance = 0;
        public static readonly int _SideStepLeft = 731208140;
        public static readonly int _SideStepRight = 1696653403;

        //
        // weapon states
        //
        public static readonly int _WeaponIdle = -1479210739;
        public static readonly int _CheckMag = 351790385;
        public static readonly int _EjectLastRound = -1717115204;

        // reload states
        public static readonly int _ReachForMag = 1898839857;
        public static readonly int _HandOnMag = 1051831248;
        //
        public static readonly int _PullMag = -1191338705;
        //
        public static readonly int _ReplaceMag = -1821557201;
        public static readonly int _ReturnHandAfterCharging = -507049588;
        public static readonly int _ReturnHandWithoutCharging = 32228103;
        public static readonly int _ReturnHandNoMag = 1392741434;
        //
        public static readonly int _WithdrawWeapon = -1205675207;
        public static readonly int _PresentWeapon = -965243548;

        static int _currentBodyStateHash = 0;

        public static void SetCurrentBodyAnimState(int state)
        {
            _currentBodyStateHash = state;
        }
        public static void SetCurrentWeaponAnimState(int state)
        {
            //UtilsTIRL.Log($"current weapon anim hash {state}");

            if (state == _HandOnMag)
            {
                _weaponState = EWeaponState.INTO_RELOAD;
            }
            else if (state == _PullMag)
            {
                _weaponState = EWeaponState.MID_RELOAD;
            }
            else if (state == _ReplaceMag || state == _ReturnHandAfterCharging || state == _ReturnHandWithoutCharging || state == _ReturnHandNoMag)
            {
                _weaponState = EWeaponState.OUT_OF_RELOAD;
            }
            else if (state == _CheckMag)
            {
                _weaponState = EWeaponState.CHECK_MAG;
            }
            else if (state == _WeaponIdle)
            {
                _weaponState = EWeaponState.IDLE;
            }
        }

        public static bool IsTurning
        {
            get { return _currentBodyStateHash == _TurnState1 || _currentBodyStateHash == _TurnState2; }
        }

        public static bool IsBlindfire
        {
            get { return _currentBodyStateHash == _Blindfire1 || _currentBodyStateHash == _Blindfire2; }
        }

        public static bool IsLeftShoulder
        {
            get { return _currentBodyStateHash == _LeftShoulderStance; }
        }
        public static bool IsSideStep
        {
            get { return _currentBodyStateHash == _SideStepLeft || _currentBodyStateHash == _SideStepRight; }
        }

        public static EWeaponState WeaponState => _weaponState;
    }
}
