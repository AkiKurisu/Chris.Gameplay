﻿using System;

namespace Chris.Graphics
{
    /// <summary>
    /// Implement for custom graphics module
    /// </summary>
    public abstract class GraphicsModule : IDisposable
    {
        public virtual void Initialize(GraphicsController graphics, GraphicsSettings settings)
        {

        }
        
        public virtual void Dispose()
        {

        }
    }
}