using Mutagen.Bethesda;
using Mutagen.Bethesda.Synthesis;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Order;
using Mutagen.Bethesda.Plugins.Cache;
using Noggog;
using Mutagen.Bethesda.Plugins.Records;

namespace ShatteredNPCpatch
{
    public class Program
    {
        private static List<FormKey> _xMarkers = new List<FormKey>();
        public static async Task<int> Main(string[] args)
        {
            return await SynthesisPipeline.Instance
                .AddPatch<ISkyrimMod, ISkyrimModGetter>(RunPatch)
                .SetTypicalOpen(GameRelease.SkyrimSE, "Shattered Skyrim - NPCs Patch.esp")
                .Run(args);
        }

        public static void RunPatch(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            if (state.LoadOrder.TryGetValue("Alternate Skyrim.esp", out var shattered) && shattered.Mod != null)
            {
                //Form Keys for placed NPC xMarkers
                _xMarkers.Add(FormKey.Factory("005901:Alternate Skyrim.esp"));
                _xMarkers.Add(FormKey.Factory("005903:Alternate Skyrim.esp"));
                _xMarkers.Add(FormKey.Factory("0290E7:Alternate Skyrim.esp"));
                _xMarkers.Add(FormKey.Factory("441F10:Alternate Skyrim.esp"));
                _xMarkers.Add(FormKey.Factory("4B1554:Alternate Skyrim.esp"));
                _xMarkers.Add(FormKey.Factory("023FE3:Alternate Skyrim.esp"));

                Console.WriteLine("--------------------------");
                PatchBaseNpcs(state, shattered);    
                Console.WriteLine("--------------------------");
            }
            else
                throw new Exception("This Patcher requires Shattered Skyrim to be installed (Alternate Skyrim.esp)" +
                    "/n if you do not have this plugin the patcher is useless");

        }

        private static List<IModListing<ISkyrimModGetter>> FilterShatteredPatch
            (
            IPatcherState<ISkyrimMod, ISkyrimModGetter> state, 
            IModListing<ISkyrimModGetter> shattered
            )
        {
            List<IModListing<ISkyrimModGetter>> mods = new List<IModListing<ISkyrimModGetter>>();

            foreach(var plugin in state.LoadOrder.PriorityOrder)
            {
                var _masterCollection = plugin.Mod;
                if (_masterCollection == null) { continue; }

                foreach (var master in _masterCollection.MasterReferences)
                    if (master.Master == shattered.ModKey)
                    {
                        mods.Add(plugin);
                        Console.WriteLine(plugin.FileName);
                        break;
                    }                
            }

            return mods;
        }

        private static void PatchBaseNpcs(IPatcherState<ISkyrimMod, ISkyrimModGetter> state, 
            IModListing<ISkyrimModGetter> mod)
        {
            if (mod.Mod == null)
            { Console.WriteLine("problem"); return; }

            var contextList = new List<IModContext<ISkyrimMod, ISkyrimModGetter,
                IPlacedNpc, IPlacedNpcGetter>>();

            foreach (var context in mod.Mod.EnumerateMajorRecordContexts
                <IPlacedNpc, IPlacedNpcGetter>(mod.Mod.ToImmutableLinkCache()))
            {
                var parent = context.Record.EnableParent;
                if (parent != null && _xMarkers.Contains(parent.Reference.FormKey))
                {
                    Console.WriteLine("Record is" + context.Record);
                    context.GetOrAddAsOverride(state.PatchMod);
                    contextList.Add(context);
                }
            }

            var patches = FilterShatteredPatch(state, mod);
            foreach (var patch in patches)
                ApplyPatch(state, patch, contextList);
        }

        private static void ApplyPatch(IPatcherState<ISkyrimMod, ISkyrimModGetter> state, IModListing<ISkyrimModGetter> mod, 
            List<IModContext<ISkyrimMod, ISkyrimModGetter, IPlacedNpc, IPlacedNpcGetter>> contextParent)
        {
            if (mod.Mod == null)
            { Console.WriteLine("problem"); return; }

            foreach (var context in mod.Mod.EnumerateMajorRecordContexts
                <IPlacedNpc, IPlacedNpcGetter>(mod.Mod.ToImmutableLinkCache()))
            {
                Console.WriteLine("Record is" + context.Record);
                if (contextParent.Contains(context))
                {
                    context.GetOrAddAsOverride(state.PatchMod);
                    Console.Write(" OVERIDDEN");
                }
            }
        }
    }

}
