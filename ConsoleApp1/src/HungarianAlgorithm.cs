

using System.Collections.Generic;

namespace GraphAlgorithms
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class HungarianAlgorithm
    {
        private readonly double[,] _costMatrix;
        private double _inf;
        private int _n; //number of elements
        private double[] _lx; //labels for workers
        private double[] _ly; //labels for jobs 
        private bool[] _s;
        private bool[] _t;
        private double[] _matchX; //vertex matched with x
        private double[] _matchY; //vertex matched with y
        private int _maxMatch;
        private double[] _slack;
        private double[] _slackx;
        private double[] _prev; //memorizing paths

        /// <summary>
        /// 
        /// </summary>
        /// <param name="costMatrix"></param>
        public HungarianAlgorithm(double[,] costMatrix)
        {
            _costMatrix = costMatrix;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public double[] Run()
        {
            _n = _costMatrix.GetLength(0);

            _lx = new double[_n];
            _ly = new double[_n];
            _s = new bool[_n];
            _t = new bool[_n];
            _matchX = new double[_n];
            _matchY = new double[_n];
            _slack = new double[_n];
            _slackx = new double[_n];
            _prev = new double[_n];
            _inf = double.MaxValue;


            InitMatches();

            if (_n != _costMatrix.GetLength(1))
                return null;

            InitLbls();

            _maxMatch = 0;

            InitialMatching();

            var q = new Queue<double>();

            #region augment

            while (_maxMatch != _n)
            {
                q.Clear();

                InitSt();
                //Array.Clear(S,0,n);
                //Array.Clear(T, 0, n);


                //parameters for keeping the position of root node and two other nodes
                var root = 0;
                int x;
                var y = 0;

                //find root of the tree
                for (x = 0; x < _n; x++)
                {
                    if (_matchX[x] != -1) continue;
                    q.Enqueue(x);
                    root = x;
                    _prev[x] = -2;

                    _s[x] = true;
                    break;
                }

                //init slack
                for (var i = 0; i < _n; i++)
                {
                    _slack[i] = _costMatrix[root, i] - _lx[root] - _ly[i];
                    _slackx[i] = root;
                }

                //finding augmenting path
                while (true)
                {
                    while (q.Count != 0)
                    {
                        x = (int)q.Dequeue();
                        var lxx = _lx[x];
                        for (y = 0; y < _n; y++)
                        {
                            if (_costMatrix[x, y] != lxx + _ly[y] || _t[y]) continue;
                            if (_matchY[y] == -1) break; //augmenting path found!
                            _t[y] = true;
                            q.Enqueue((int)_matchY[y]);

                            AddToTree((int)_matchY[y], x);
                        }
                        if (y < _n) break; //augmenting path found!
                    }
                    if (y < _n) break; //augmenting path found!
                    UpdateLabels(); //augmenting path not found, update labels

                    for (y = 0; y < _n; y++)
                    {
                        //in this cycle we add edges that were added to the equality graph as a
                        //result of improving the labeling, we add edge (slackx[y], y) to the tree if
                        //and only if !T[y] &&  slack[y] == 0, also with this edge we add another one
                        //(y, yx[y]) or augment the matching, if y was exposed

                        if (_t[y] || _slack[y] != 0) continue;
                        if (_matchY[y] == -1) //found exposed vertex-augmenting path exists
                        {
                            x = (int)_slackx[y];
                            break;
                        }
                        _t[y] = true;
                        if (_s[(int)_matchY[y]]) continue;
                        q.Enqueue(_matchY[y]);
                        AddToTree((int)_matchY[y], (int)_slackx[y]);
                    }
                    if (y < _n) break;
                }

                _maxMatch++;

                //inverse edges along the augmenting path
                int ty;
                for (int cx = x, cy = y; cx != -2; cx = (int)_prev[cx], cy = ty)
                {
                    ty = (int)_matchX[cx];
                    _matchY[cy] = cx;
                    _matchX[cx] = cy;
                }
            }

            #endregion

            return _matchX;
        }

        private void InitMatches()
        {
            for (var i = 0; i < _n; i++)
            {
                _matchX[i] = -1;
                _matchY[i] = -1;
            }
        }

        private void InitSt()
        {
            for (var i = 0; i < _n; i++)
            {
                _s[i] = false;
                _t[i] = false;
            }
        }

        private void InitLbls()
        {
            for (var i = 0; i < _n; i++)
            {
                var minRow = _costMatrix[i, 0];
                for (var j = 0; j < _n; j++)
                {
                    if (_costMatrix[i, j] < minRow) minRow = _costMatrix[i, j];
                    if (minRow == 0) break;
                }
                _lx[i] = minRow;
            }
            for (var j = 0; j < _n; j++)
            {
                var minColumn = _costMatrix[0, j] - _lx[0];
                for (var i = 0; i < _n; i++)
                {
                    if (_costMatrix[i, j] - _lx[i] < minColumn) minColumn = _costMatrix[i, j] - _lx[i];
                    if (minColumn == 0) break;
                }
                _ly[j] = minColumn;
            }
        }

        private void UpdateLabels()
        {
            var delta = _inf;
            for (var i = 0; i < _n; i++)
                if (!_t[i])
                    if(delta>_slack[i])
                        delta = _slack[i];
            for (var i = 0; i < _n; i++)
            {
                if (_s[i])
                    _lx[i] = _lx[i] + delta;
                if (_t[i])
                    _ly[i] = _ly[i] - delta;
                else _slack[i] = _slack[i] - delta;
            }
        }

        private void AddToTree(int x, int prevx)
        {
            //x-current vertex, prevx-vertex from x before x in the alternating path,
            //so we are adding edges (prevx, matchX[x]), (matchX[x],x)

            _s[x] = true; //adding x to S
            _prev[x] = prevx;

            var lxx = _lx[x];
            //updateing slack
            for (var y = 0; y < _n; y++)
            {
                if (_costMatrix[x, y] - lxx - _ly[y] >= _slack[y]) continue;
                _slack[y] = _costMatrix[x, y] - lxx - _ly[y];
                _slackx[y] = x;
            }
        }

        private void InitialMatching()
        {
            for (var x = 0; x < _n; x++)
            {
                for (var y = 0; y < _n; y++)
                {
                    if (_costMatrix[x, y] != _lx[x] + _ly[y] || _matchY[y] != -1) continue;
                    _matchX[x] = y;
                    _matchY[y] = x;
                    _maxMatch++;
                    break;
                }
            }
        }
    }
}
