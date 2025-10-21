using SimQCore;

namespace SimQCore.Library.CompareDists {

    public abstract class CompareDistsTests
    {
        //Each test has the ReflectionType
        public Type_Name name = new();
        public CompareDistsTests() 
        {
            name.Type = GetType().Name;
            name.Name = name.Type.ToString();
        }
        
    public abstract bool CompareDists(double[] dist1, double[] dist2, out double result);

    }

}




