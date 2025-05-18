using UnityEngine;

namespace WaveFunctionCollapse
{
    public class PatternData
    {//Pattern-uri din input
        private Pattern pattern;//pattern-ul în sine (sub-grila N×N cu indici)
        private int frequency = 1;//cate aparitii ale acestui pattern s-au găsit în input
        private float frequencyRelative;//frecvența relativă NORMALIZATA= frequency / totalul tuturor pattern-urilor
        private float frequencyRelativeLog2;//logaritmul în baza 2 din (frecventa relativa NORMALIZATA); Se foloseste pt entropie

        //Get methods:
        public float FrequencyRelative { get => frequencyRelative; }
        public Pattern Pattern { get => pattern; }
        public float FrequencyRelativeLog2 { get => frequencyRelativeLog2; }

        public PatternData(Pattern pattern)
        {
            this.pattern = pattern;
            // la început _frequency = 0, relativ/log2=0
        }

        public void AddToFrequency()
        {
            frequency++;
        }

        public void CalculateRelativeFrequency(int total)//totalul e acelasi lucru cu suma frecventelor/probabilitatilor pt patterns -> folosim la NORMALIZARE
        {
            frequencyRelative = (float)frequency / total;
            frequencyRelativeLog2 = Mathf.Log(frequencyRelative, 2);
        }

        public bool CompareGrid(Direction dir, PatternData data)
        {
            return pattern.ComparePatternToAnotherPattern(dir, data.Pattern);
        }
    }
}
