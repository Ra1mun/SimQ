using System;
using System.Collections.Generic;
using System.Linq;

namespace SimQCore.Library.Events {
    class ExhaustiveEvents {
        private BaseSensor _baseSensor;
        private List<double> _arraySumP = new();

        public ExhaustiveEvents( double [] arrayP ) {
            _baseSensor = new BaseSensor();
            double sum = 0;
            for( int i = 0; i < arrayP.Length; i++ ) {
                sum += arrayP [i];
                _arraySumP [i] = sum;
            }
        }

        public int Generate() {
            var randomP = _baseSensor.Next();
            var nearest = _arraySumP.Aggregate((current, next)
                => Math.Abs((long)current - randomP) < Math.Abs((long)next - randomP) ? current : next);
            return _arraySumP.IndexOf( nearest );
        }
    }
}
