using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Graphics.RSI;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Content.Shared.YARtech;

namespace Content.Client.YARtech.AnimatedBackground
{
    public sealed class AnimatedBackgroundControl : TextureRect
    {
        [Dependency] private readonly IResourceCache _resourceCache = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        private string _rsiPath = "/Textures/White/Lobby/native.rsi";

        public RSI? _RSI;

        private const int States = 1;

        private readonly BackgroundData[] _data = new BackgroundData[1];

        public AnimatedBackgroundControl()
        {
            IoCManager.InjectDependencies(this);
            InitializeStates();
        }

        private void InitializeStates()
        {
            if (_RSI == null)
            {
                _RSI = _resourceCache.GetResource<RSIResource>(_rsiPath).RSI;
            }
            for (int i = 0; i < 1; i++)
            {
                if (_RSI.TryGetState((i + 1).ToString(), out var state))
                {

                    Texture[] frames = state.GetFrames(RsiDirection.South);
                    float[] delays = state.GetDelays();
                    Frame[] frameData = frames.Select((Texture texture, int index) => new Frame(texture, delays[index])).ToArray();
                    _data[i] = new BackgroundData(frameData);
                }
            }
        }

        public void SetRSI(RSI? rsi)
        {
            _RSI = rsi;
            InitializeStates();
        }

        protected override void FrameUpdate(FrameEventArgs args)
        {
            base.FrameUpdate(args);
            BackgroundData[] data = _data;
            foreach (BackgroundData backData in data)
            {
                Frame frame = backData.Current();
                backData.Timer += args.DeltaSeconds;
                if (!(backData.Timer < frame.Delay))
                {
                    backData.Timer = 0f;
                    base.Texture = frame.Texture;
                    backData.Next();
                }
            }
        }

        public void RandomizeBackground()
        {
            List<AnimatedLobbyScreenPrototype> backgroundsProto = _prototypeManager.EnumeratePrototypes<AnimatedLobbyScreenPrototype>().ToList();
            int index = new Random().Next(backgroundsProto.Count);
            _rsiPath = "/Textures/" + backgroundsProto[index].Path;
            InitializeStates();
        }
    }
}
