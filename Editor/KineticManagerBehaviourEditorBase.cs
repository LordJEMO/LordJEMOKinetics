using LordJEMO.Kinematics;
using UnityEditor;
using UnityEngine;

namespace LordJEMO.Editor.Kinematics
{
    /// <summary>
    ///     Shared inspector for <see cref="KineticManagerBehaviourBase"/>: the default fields plus a
    ///     Setup box with a live trajectory / entry read-out and play-mode buttons that drive the demo
    ///     harness (add trajectories from the assigned assets and spawn cubes, clear the entries).
    /// </summary>
    public abstract class KineticManagerBehaviourEditorBase : UnityEditor.Editor
    {
        public override bool RequiresConstantRepaint()
        {
            return Application.isPlaying;
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            KineticManagerBehaviourBase behaviour = (KineticManagerBehaviourBase)target;

            EditorGUILayout.Space();
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Setup", EditorStyles.boldLabel);

                if (Application.isPlaying)
                {
                    EditorGUILayout.LabelField("Trajectories", behaviour.Registry.TrajectoryCount.ToString());
                    EditorGUILayout.LabelField("Entries", behaviour.Manager.Count.ToString());
                }
                else
                {
                    EditorGUILayout.HelpBox("Enter Play Mode to add trajectories and spawn entries.", MessageType.Info);
                }

                using (new EditorGUI.DisabledScope(!Application.isPlaying))
                {
                    if (GUILayout.Button("Build Demo (trajectories + entries)"))
                    {
                        behaviour.BuildDemo();
                    }

                    if (GUILayout.Button("Clear"))
                    {
                        behaviour.ClearDemo();
                    }
                }
            }
        }
    }
}
