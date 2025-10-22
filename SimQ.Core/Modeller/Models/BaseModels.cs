using SimQ.Domain.Models.ProblemAggregation;

namespace SimQCore.Modeller.Models {

    

    public interface IModellingAgent {
        public string Id {
            get;
            set;
        }
        public BaseCall DoEvent( double T );
        public double NextEventTime {
            get;
        }
        public string EventTag {
            get;
        }
        public AgentType Type {
            get;
        }
        public bool IsActive();
    }

    public abstract class BaseSource: IModellingAgent {
        private static int idCounter;
        public virtual string Id {
            get; set;
        }
        public abstract double NextEventTime {
            get;
        }
        public abstract string EventTag {
            get;
        }
        public AgentType Type { get; } = AgentType.SOURCE;
        public abstract BaseCall DoEvent( double T );
        public abstract bool IsActive();
        public abstract int CallsCreated {
            get;
        }
    }

    public abstract class BaseServiceBlock: IModellingAgent {
        private static int idCounter;
        public virtual string Id {
            get; set;
        }
        public abstract double NextEventTime {
            get;
        }
        public abstract string EventTag {
            get;
        }
        public AgentType Type { get; } = AgentType.SERVICE_BLOCK;
        public abstract BaseCall ProcessCall {
            get;
        }
        public abstract BaseCall DoEvent( double T );
        public abstract bool IsActive();
        public abstract bool IsFree();
        public abstract List<BaseBuffer> BindedBuffers { get; set; }
        public abstract void BindBuffer( BaseBuffer buffer );
        public abstract bool TakeCall( BaseCall call, double T );
    }

    public abstract class BaseBuffer: IModellingAgent {
        private static int idCounter;
        public virtual string Id {
            get; set;
        }
        public abstract double NextEventTime {
            get;
        }
        public abstract string EventTag {
            get;
        }
        public AgentType Type { get; } = AgentType.BUFFER;
        public abstract bool IsFull {
            get;
        }
        public abstract bool IsEmpty {
            get;
        }
        public abstract bool TakeCall( BaseCall call );
        public abstract BaseCall PassCall();
        public abstract BaseCall DoEvent( double T );
        public abstract bool IsActive();
        public abstract int CurrentSize {
            get;
        }
    }

    public abstract class BaseCall: IModellingAgent {
        private static int idCounter;
        public virtual string Id {
            get; set;
        }
        public abstract double NextEventTime {
            get;
        }
        public abstract string EventTag {
            get;
        }
        public AgentType Type { get; } = AgentType.CALL;
        public abstract BaseCall DoEvent( double T );
        public abstract bool IsActive();
    }

    public abstract class BaseOrbit: IModellingAgent {
        private static int idCounter;
        public virtual string Id {
            get; set;
        }
        public abstract double NextEventTime {
            get;
        }
        public abstract string EventTag {
            get;
        }
        public AgentType Type { get; } = AgentType.ORBIT;
        public abstract BaseCall DoEvent( double T );
        public abstract bool IsActive();
        public abstract bool TakeCall( BaseCall call, double T );
        public abstract BaseCall PeekNextCall( double T );
        public abstract int CurrentSize {
            get;
        }
    }
}
