using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProgressSystem.GameEvents
{
    public abstract class GameEvent
    {
        public virtual bool IsCompleted { get; protected set; }

        public event Action<GameEvent> OnCompleted;
        protected ref Action<GameEvent> _onCompleted => ref OnCompleted;
        protected virtual void Complete()
        {
            if (IsCompleted)
            {
                return;
            }
            IsCompleted = true;
            OnCompleted?.Invoke(this);
        }
    }
}
