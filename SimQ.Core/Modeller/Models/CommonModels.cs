using SimQ.Core.Models;
using SimQ.Core.Models.Base;
using SimQ.Core.Models.Distributions;
using SimQ.Domain.Models.ProblemAggregation;
using SimQCore;
using SimQCore.Modeller;

namespace SimQ.Core.Modeller.Models {
    public class Source: BaseSource {
        private readonly IDistribution _distribution;

        protected int _callCounter;

        protected double _tau;

        private readonly Func<IModellingAgent, List<IModellingAgent>, double, bool> EventAction = ( Agent, Links, T ) => {
            BaseCall call = Agent.DoEvent( T );

            foreach( IModellingAgent serviceBlock in Links ) {
                if( serviceBlock.Type == AgentType.SERVICE_BLOCK ) {
                    if ( ( ( BaseServiceBlock )serviceBlock ).IsFree() ) {
                        ( ( BaseServiceBlock )serviceBlock ).TakeCall( call, T );
                        return true;
                    }
                }
            }

            foreach( IModellingAgent serviceBlock in Links ) {
                if( serviceBlock.Type == AgentType.SERVICE_BLOCK ) {
                    if ( ( ( BaseServiceBlock )serviceBlock ).TakeCall( call, T ) ) {
                        return true;
                    }
                }
            }

            foreach( IModellingAgent orbit in Links ) {
                if( orbit.Type == AgentType.ORBIT ) {
                    if ( ( ( BaseOrbit )orbit ).TakeCall( call, T ) ) {
                        return true;
                    }
                }
            }

            Misc.Log( $"\nЗаявка { call.Id } не попала в систему.", LogStatus.ERROR );

            return false;
        };

        public Source( IDistribution distribution ) : base() {
            _distribution = distribution;
            _tau = CalcNextEventTime( 0 );

            Supervisor.AddAction( EventTag, EventAction );
        }
        public override string EventTag => "Source";

        protected BaseCall CreateCall() {
            BaseCall call = new Call() {
                Id = "CALL_" + Id + "_" + _callCounter++
            };
            return call;
        }

        public override string Id {
            get; set;
        }

        public override BaseCall DoEvent( double T ) {
            CalcNextEventTime( T );
            BaseCall call = CreateCall();

            Misc.Log( $"\nМодельное время: {T}, агент: {Id}, заявка {call.Id} поступила.", LogStatus.SUCCESS );
            return call;
        }

        public override double NextEventTime => _tau;

        protected double CalcNextEventTime( double T ) {
            _tau = T + _distribution.Generate();
            return _tau;
        }

        public override bool IsActive() => true;

        public override int CallsCreated => _callCounter;
    }

    internal class ServiceBlock: BaseServiceBlock {
        private readonly IDistribution _distribution;

        private double _delta = double.PositiveInfinity;

        private BaseCall _processCall;

        public override List<BaseBuffer> BindedBuffers { get; set; }

        private readonly Func<IModellingAgent, List<IModellingAgent>, double, bool> EventAction = (Agent, _, T) => {
            BaseCall call = Agent.DoEvent( T );
            call.DoEvent( T );
            return true;
        };

        public ServiceBlock( IDistribution distribution ) : base() {
            _distribution = distribution;

            Supervisor.AddAction( EventTag, EventAction );
        }

        public override BaseCall ProcessCall => _processCall;
        public override bool IsFree() => _processCall == null;
        public override string EventTag => GetType().Name;
        public override double NextEventTime => _delta;
        public override void BindBuffer( BaseBuffer buffer ) => BindedBuffers.Add( buffer );

        private bool AcceptCall( BaseCall call, double T ) {
            Misc.Log( $"Заявка {call.Id} принята в обработку устройством {Id}." );
            _processCall = call;
            _delta = gS( T );
            return true;
        }

        private double gS( double T ) => T + _distribution.Generate();

        private BaseCall EndProcessCall() {
            BaseCall temp = _processCall;
            _processCall = null;
            _delta = double.PositiveInfinity;
            return temp;
        }

        private BaseCall GetCallFromBuffer() {
            foreach( BaseBuffer buffer in BindedBuffers ) {
                if( buffer.IsEmpty ) {
                    continue;
                }

                return buffer.PassCall();
            }
            return null;
        }

        private void TakeNextCall( double T ) {
            BaseCall nextCall = GetCallFromBuffer();
            if( nextCall != null ) {
                AcceptCall( nextCall, T );
            }
        }

        public override BaseCall DoEvent( double T ) {
            BaseCall temp = EndProcessCall();

            Misc.Log( $"\nМодельное время: {T}, агент: {Id}, заявка {temp.Id} обработана.", LogStatus.SUCCESS );

            TakeNextCall( T );

            return temp;
        }

        private bool SendToBuffer( BaseCall call, double _ ) {
            foreach( BaseBuffer buffer in BindedBuffers ) {
                if( buffer.TakeCall( call ) ) {
                    Misc.Log( $"Заявка {call.Id} попала в буфер {buffer.Id}." );
                    return true;
                }
            }

            return false;
        }

        public override bool TakeCall( BaseCall call, double T ) => IsFree() ? AcceptCall( call, T ) : SendToBuffer( call, T );

        public override bool IsActive() => true;
    }

    internal class StackBuffer: BaseBuffer, IAgentStatistic {
        private int _capacity = 0;

        private readonly Stack<BaseCall> _calls = new();

        public StackBuffer( int capacity = 0 ) : base() => _capacity = capacity;
        public override bool IsEmpty => _calls.Count == 0;
        public override bool IsFull => ( _capacity != 0 ) && ( _calls.Count >= _capacity );
        public override BaseCall PassCall() => IsEmpty ? null : _calls.Pop();

        public override bool TakeCall( BaseCall newCall ) {
            if( IsFull ) {
                return false;
            }

            _calls.Push( newCall );
            return true;
        }

        public override BaseCall DoEvent( double T ) => null;
        public override bool IsActive() => false;
        public override double NextEventTime => double.PositiveInfinity;
        public override string EventTag => GetType().Name;
        public override int CurrentSize => _calls.Count;
        /** Текущим состоянием является количество заявок в буфере в момент обращения к агенту. */
        public int GetCurrentState() => CurrentSize;
    }

    public class QueueBuffer: BaseBuffer, IAgentStatistic {
        private int _capacity;

        private readonly Queue<BaseCall> _calls = new();

        /**
         * Конструктор. Принимает число, отражающее максимальное количество заявок в очереди.
         * По умолчанию - очередь не органичена.
         */
        public QueueBuffer( int capacity = int.MaxValue ) : base() => _capacity = capacity;
        public override bool IsEmpty => _calls.Count == 0;
        public override bool IsFull => ( _capacity != int.MaxValue ) && ( _calls.Count >= _capacity );
        public override BaseCall PassCall() => IsEmpty ? null : _calls.Dequeue();
        public override bool TakeCall( BaseCall newCall ) {
            if( IsFull ) {
                return false;
            }

            _calls.Enqueue( newCall );
            return true;
        }

        public override BaseCall DoEvent( double T ) => null;
        public override bool IsActive() => false;
        public override double NextEventTime => double.PositiveInfinity;
        public override string EventTag => GetType().Name;
        public override int CurrentSize => _calls.Count;
        /** Текущим состоянием является количество заявок в буфере в момент обращения к агенту. */
        public int GetCurrentState() => CurrentSize;
    }

    internal class Call: BaseCall {
        public override BaseCall DoEvent( double T ) => this;
        public override bool IsActive() => false;
        public override double NextEventTime => double.PositiveInfinity;
        public override string EventTag => GetType().Name;
    }

    internal class Orbit: BaseOrbit {
        private readonly Queue<BaseCall> _calls = new();
        private readonly IDistribution _distribution;
        private double _teta = double.PositiveInfinity;
        private readonly Func<IModellingAgent, List<IModellingAgent>, double, bool> EventAction = (Agent, Links, T) => {
            BaseCall call = ( ( BaseOrbit )Agent ).PeekNextCall( T );

            Misc.Log( $"\nМодельное время: { T }, агент: { Agent.Id }, попытка возврата заявки { call.Id } в систему.", LogStatus.WARNING );

            foreach( BaseServiceBlock serviceBlock in Links ) {
                if( serviceBlock.Type == AgentType.SERVICE_BLOCK ) {
                    if( serviceBlock.TakeCall( call, T ) ) {
                        Agent.DoEvent( T );
                        return true;
                    }
                }
            }

            return false;
        };
        private void CalcNextEventTime( double T ) => _teta = _calls.Count == 0
                                                                ? double.PositiveInfinity
                                                                : T + _distribution.Generate();
        public Orbit( IDistribution distribution ) : base() {
            _distribution = distribution;
            Supervisor.AddAction( EventTag, EventAction );
        }
        public override double NextEventTime => _teta;
        public override string EventTag => GetType().Name;
        public override BaseCall DoEvent( double T ) {
            BaseCall call = _calls.Dequeue();
            CalcNextEventTime( T );

            return call;
        }
        public override BaseCall PeekNextCall( double T ) {
            CalcNextEventTime( T );
            return _calls.Peek();
        }
        public override bool IsActive() => true;
        public override bool TakeCall( BaseCall call, double T ) {
            _calls.Enqueue( call );
            if( _calls.Count == 1 ) {
                CalcNextEventTime( T );
            }

            return true;
        }
        public override int CurrentSize => _calls.Count;
    }

    internal class PollingServiceBlock: BaseServiceBlock {
        private readonly IDistribution _distribution;

        private double _delta = double.PositiveInfinity;

        private BaseCall _processCall;

        public override List<BaseBuffer> BindedBuffers { get; set; }

        private int _currentPollingBufferInd = 0;

        private double _pollingPeriod;

        private double _nextPollingBufferDelta;

        private readonly Func<IModellingAgent, List<IModellingAgent>, double, bool> EventAction = (Agent, _, T) => {
            BaseCall call = Agent.DoEvent( T );
            return true;
        };

        public PollingServiceBlock( IDistribution distribution, double pollingPeriod ) : base() {
            _nextPollingBufferDelta = _pollingPeriod = pollingPeriod;
            _distribution = distribution;

            Supervisor.AddAction( EventTag, EventAction );
        }

        public override BaseCall ProcessCall => _processCall;
        public override bool IsFree() => _processCall == null;
        public override string EventTag => GetType().Name;
        public override double NextEventTime => Math.Min( _delta, _nextPollingBufferDelta );
        public override void BindBuffer( BaseBuffer buffer ) => BindedBuffers.Add( buffer );

        private bool AcceptCall( BaseCall call, double T ) {
            Misc.Log( $"Заявка {call.Id} принята в обработку устройством {Id}." );
            _processCall = call;
            _delta = gS( T );
            return true;
        }

        private double gS( double T ) => T + _distribution.Generate();

        private BaseCall EndProcessCall() {
            BaseCall temp = _processCall;
            _processCall = null;
            _delta = double.PositiveInfinity;
            return temp;
        }

        private BaseCall GetCallFromBuffer() {
            BaseBuffer buffer = BindedBuffers[ _currentPollingBufferInd ];
            return buffer.IsEmpty ? null : buffer.PassCall();
        }

        private void TakeNextCall( double T ) {
            BaseCall nextCall = GetCallFromBuffer();
            if( nextCall != null ) {
                AcceptCall( nextCall, T );
            }
        }

        public override BaseCall DoEvent( double T ) {
            if( _nextPollingBufferDelta < _delta ) {
                SetNextPollingBuffer();
                _nextPollingBufferDelta += _pollingPeriod;
                TakeNextCall( T );

                return null;
            } else {
                BaseCall temp = EndProcessCall();
                Misc.Log( $"\nМодельное время: {T}, агент: {Id}, заявка {temp.Id} обработана.", LogStatus.SUCCESS );
                TakeNextCall( T );

                return temp;
            }
        }

        private void SetNextPollingBuffer() {
            _currentPollingBufferInd = ( _currentPollingBufferInd + 1 ) % BindedBuffers.Count;
            Misc.Log( $"\nМодельное время: {_nextPollingBufferDelta}, " +
                $"агент: {Id} меняет опрашиваемый буфер на {BindedBuffers [_currentPollingBufferInd].Id}.", LogStatus.WARNING );
        }

        private bool SendToBuffer( BaseCall call, double _ ) {
            foreach( BaseBuffer buffer in BindedBuffers ) {
                if( buffer.TakeCall( call ) ) {
                    Misc.Log( $"Заявка {call.Id} попала в буфер {buffer.Id}." );
                    return true;
                }
            }

            return false;
        }

        public override bool TakeCall( BaseCall call, double T ) {
            if( SendToBuffer( call, T ) ) {
                if( IsFree() ) {
                    TakeNextCall( T );
                }

                return true;
            } else {
                return false;
            }
        }

        public override bool IsActive() => true;
    }

    internal class FiniteSource: Source {
        public bool isBlocked {
            get; private set;
        }

        public void unBlock( double T ) {
            isBlocked = false;
            CalcNextEventTime( T );
        }

        protected new BaseCall CreateCall() {
            FiniteCall call = new FiniteCall( this ) {
                Id = "CALL_" + Id + "_" + _callCounter++
            };

            return call;
        }

        public override BaseCall DoEvent( double T ) {
            CalcNextEventTime( T );
            BaseCall call = CreateCall();

            Misc.Log( $"\nМодельное время: {T}, агент: {Id}, заявка {call.Id} поступила.", LogStatus.SUCCESS );

            isBlocked = true;
            Misc.Log( $"Входящий поток {Id} заблокирован." );

            return call;
        }

        public FiniteSource( IDistribution distribution ) : base( distribution ) { }
        public override string EventTag => GetType().Name;

        public override double NextEventTime => isBlocked ? double.PositiveInfinity : _tau;
    }

    internal class FiniteCall: Call {
        private FiniteSource _callOwner;

        public FiniteCall( FiniteSource owner ) : base() {
            _callOwner = owner;
        }
        public override BaseCall DoEvent( double T ) {
            Misc.Log( $"Заявка {Id} покинула систему: источник заявок {_callOwner.Id} разблокирован.", LogStatus.WARNING );
            _callOwner?.unBlock( T );

            return this;
        }
        public override string EventTag => "Call";
    }
}
