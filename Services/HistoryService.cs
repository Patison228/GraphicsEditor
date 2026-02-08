using System.Collections.Generic;
using System.Windows.Media.Imaging;

namespace GraphicsEditor.Services
{

    public class HistoryService
    {
        private readonly Stack<RenderTargetBitmap> _history = new Stack<RenderTargetBitmap>();
        private const int MaxHistorySize = 20;

        public void SaveState(RenderTargetBitmap bitmap)
        {
            _history.Push(bitmap);

            if (_history.Count > MaxHistorySize)
            {
                var temp = new Stack<RenderTargetBitmap>();
                for (int i = 0; i < MaxHistorySize; i++)
                {
                    temp.Push(_history.Pop());
                }
                _history.Clear();
                while (temp.Count > 0)
                {
                    _history.Push(temp.Pop());
                }
            }
        }

        public RenderTargetBitmap Undo()
        {
            if (_history.Count > 1)
            {
                _history.Pop(); 
                return _history.Peek(); 
            }
            return null;
        }

        public bool CanUndo => _history.Count > 1;

        public void Clear()
        {
            _history.Clear();
        }
    }
}