using SimQCore.Modeller.Models;

namespace SimQ.Core.Statistic
{
    public class StatesStatistic
    {
        public Dictionary<IModellingAgent, Dictionary<int, double>> states;

        public StatesStatistic(DataCollector data)
        {
            states = data.agentsStatisticData;

            NormalizeStatesProbs(ref states, data.CurrentModelationTime);
        }


        private void NormalizeStatesProbs(ref Dictionary<IModellingAgent, Dictionary<int, double>> states, double totalTime)
        {
            foreach (IModellingAgent agent in states.Keys)
                foreach (int i in states[agent].Keys)
                    states[agent][i] /= totalTime;
        }


        //X = 0,1,2,... Массив X должен начинаться с 0 и увеличиваться на 1
        public bool Get_EmpDist(out double[] Y)
        {
            int length = 0;
            Y = new double[length];

            if (states == null || states.Count == 0)
            {
                //вывод ошибки
                //Console.WriteLine("Данные эмпирической функции распределения не определены.");
                return false;
            }
            else
            {
                foreach (IModellingAgent agent in states.Keys)
                {
                    length += states[agent].Count;
                    Array.Resize(ref Y, length);

                    foreach (int i in states[agent].Keys)
                    {
                        Y[i] += states[agent][i];
                    }

                }

                return true;
            }
        }


        public void Print_EmpDist()
        {
            Console.WriteLine();
            if( states == null || states.Count == 0)
            {
                Console.WriteLine("Данные эмпирической функции распределения не определены.");
            }
            else
            {
                foreach (IModellingAgent agent in states.Keys)
                {
                    Console.WriteLine($"Данные эмпирической функции распределения {agent.Id}:");
                    foreach (int i in states[agent].Keys)
                        Console.WriteLine(string.Format("{0} {1:0.00000} ", i, states[agent][i]));
                }
            }
        }

        public string EmpDistToString() {
            string txt = "";
            if( states == null || states.Count == 0 ) {
                txt += "Данные эмпирической функции распределения не определены.";
            } else {
                foreach( IModellingAgent agent in states.Keys ) {
                    txt += $"\nДанные эмпирической функции распределения {agent.Id}:";
                    foreach( int i in states [agent].Keys ) {
                        txt += string.Format( "\n{0} {1:0.00000} ", i, states [agent] [i] );
                    }
                }
            }
            return txt;
        }

        /*
        // Save and Get Empirical Distribution
        public void Get_EmpDistr(Dictionary<IModellingAgent, Dictionary<int, double>> states, out double[] Y, out int[] X)
        {
            int length = 0;
            Y = new double[length];
            X = new int[length];
            int j = 0;

            if (states == null || states.Count == 0)
            {
                //вывод ошибки
                //Console.WriteLine("Данные эмпирической функции распределения не определены.");
            }
            else
            {
                foreach (IModellingAgent agent in states.Keys)
                {
                    length += states[agent].Count;
                    Array.Resize(ref Y, length);
                    Array.Resize(ref X, length);

                    foreach (int i in states[agent].Keys)
                    {
                        Y[j] += states[agent][i];
                        X[j] = i;
                        j++;
                    }

                }
            }
        }
        */
    }
}
