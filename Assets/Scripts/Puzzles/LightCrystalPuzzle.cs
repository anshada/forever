using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Forever.Characters;
using Forever.Interactables;

namespace Forever.Puzzles
{
    public class LightCrystalPuzzle : MonoBehaviour
    {
        [System.Serializable]
        public class CrystalNode
        {
            public Transform crystal;
            public Color targetColor;
            public bool isActivated;
            public LineRenderer beamRenderer;
            public List<CrystalNode> connectedNodes;
        }

        [Header("Puzzle Configuration")]
        public List<CrystalNode> crystalNodes;
        public float activationThreshold = 0.9f;
        public float beamWidth = 0.2f;
        public float pulseSpeed = 2f;
        public float colorLerpSpeed = 2f;

        [Header("Visual Effects")]
        public Material beamMaterial;
        public GameObject activationParticles;
        public GameObject completionEffect;
        
        private bool isPuzzleComplete = false;
        private Dictionary<Transform, CrystalNode> nodeMap;

        private void Awake()
        {
            InitializePuzzle();
        }

        private void InitializePuzzle()
        {
            nodeMap = new Dictionary<Transform, CrystalNode>();
            
            foreach (var node in crystalNodes)
            {
                // Initialize beam renderers
                if (node.beamRenderer == null)
                {
                    node.beamRenderer = node.crystal.gameObject.AddComponent<LineRenderer>();
                }
                
                SetupBeamRenderer(node.beamRenderer);
                nodeMap[node.crystal] = node;

                // Add interaction components
                var interactable = node.crystal.gameObject.AddComponent<CrystalInteractable>();
                interactable.Initialize(this, node);
            }
        }

        private void SetupBeamRenderer(LineRenderer renderer)
        {
            renderer.material = beamMaterial;
            renderer.startWidth = beamWidth;
            renderer.endWidth = beamWidth;
            renderer.positionCount = 2;
            renderer.enabled = false;
        }

        public void OnCrystalInteraction(CrystalNode node, CharacterType characterType)
        {
            switch (characterType)
            {
                case CharacterType.Iwaan:
                    StartCoroutine(ChangeColor(node));
                    break;
                case CharacterType.Ilan:
                    RevealConnections(node);
                    break;
                case CharacterType.Anshad:
                    AmplifyBeams(node);
                    break;
                case CharacterType.Shibna:
                    HealCrystal(node);
                    break;
                case CharacterType.Inaya:
                    RedirectBeam(node);
                    break;
            }

            CheckPuzzleCompletion();
        }

        private IEnumerator ChangeColor(CrystalNode node)
        {
            Renderer crystalRenderer = node.crystal.GetComponent<Renderer>();
            Color startColor = crystalRenderer.material.color;
            Color targetColor = node.targetColor;
            float elapsed = 0f;

            while (elapsed < 1f)
            {
                elapsed += Time.deltaTime * colorLerpSpeed;
                crystalRenderer.material.color = Color.Lerp(startColor, targetColor, elapsed);
                yield return null;
            }

            UpdateBeams(node);
        }

        private void RevealConnections(CrystalNode node)
        {
            foreach (var connectedNode in node.connectedNodes)
            {
                DrawBeam(node, connectedNode, true);
            }

            // Hide connections after a delay
            StartCoroutine(HideConnectionsAfterDelay(node, 3f));
        }

        private void AmplifyBeams(CrystalNode node)
        {
            StartCoroutine(PulseBeams(node));
        }

        private void HealCrystal(CrystalNode node)
        {
            Renderer crystalRenderer = node.crystal.GetComponent<Renderer>();
            var emission = crystalRenderer.material.GetColor("_EmissionColor");
            crystalRenderer.material.SetColor("_EmissionColor", emission * 2f);
            
            if (activationParticles != null)
            {
                Instantiate(activationParticles, node.crystal.position, Quaternion.identity);
            }
        }

        private void RedirectBeam(CrystalNode node)
        {
            node.crystal.Rotate(Vector3.up, 45f);
            UpdateBeams(node);
        }

        private void UpdateBeams(CrystalNode node)
        {
            foreach (var connectedNode in node.connectedNodes)
            {
                DrawBeam(node, connectedNode);
            }
        }

        private void DrawBeam(CrystalNode from, CrystalNode to, bool temporary = false)
        {
            LineRenderer beam = from.beamRenderer;
            beam.enabled = true;
            beam.SetPosition(0, from.crystal.position);
            beam.SetPosition(1, to.crystal.position);

            if (temporary)
            {
                StartCoroutine(FadeBeam(beam));
            }
        }

        private IEnumerator PulseBeams(CrystalNode node)
        {
            LineRenderer beam = node.beamRenderer;
            float originalWidth = beam.startWidth;
            float elapsed = 0f;

            while (elapsed < 1f)
            {
                elapsed += Time.deltaTime * pulseSpeed;
                float width = originalWidth * (1f + Mathf.Sin(elapsed * 10f) * 0.5f);
                beam.startWidth = width;
                beam.endWidth = width;
                yield return null;
            }

            beam.startWidth = originalWidth;
            beam.endWidth = originalWidth;
        }

        private IEnumerator FadeBeam(LineRenderer beam)
        {
            float elapsed = 0f;
            Color startColor = beam.material.color;
            Color endColor = new Color(startColor.r, startColor.g, startColor.b, 0f);

            while (elapsed < 1f)
            {
                elapsed += Time.deltaTime;
                beam.material.color = Color.Lerp(startColor, endColor, elapsed);
                yield return null;
            }

            beam.enabled = false;
            beam.material.color = startColor;
        }

        private IEnumerator HideConnectionsAfterDelay(CrystalNode node, float delay)
        {
            yield return new WaitForSeconds(delay);
            
            foreach (var connectedNode in node.connectedNodes)
            {
                node.beamRenderer.enabled = false;
            }
        }

        private void CheckPuzzleCompletion()
        {
            if (isPuzzleComplete) return;

            bool allNodesActivated = true;
            foreach (var node in crystalNodes)
            {
                Renderer crystalRenderer = node.crystal.GetComponent<Renderer>();
                float colorMatch = ColorMatchPercentage(crystalRenderer.material.color, node.targetColor);
                node.isActivated = colorMatch >= activationThreshold;
                
                if (!node.isActivated)
                {
                    allNodesActivated = false;
                    break;
                }
            }

            if (allNodesActivated)
            {
                CompletePuzzle();
            }
        }

        private float ColorMatchPercentage(Color current, Color target)
        {
            return 1f - (Mathf.Abs(current.r - target.r) +
                        Mathf.Abs(current.g - target.g) +
                        Mathf.Abs(current.b - target.b)) / 3f;
        }

        private void CompletePuzzle()
        {
            isPuzzleComplete = true;
            
            if (completionEffect != null)
            {
                Instantiate(completionEffect, transform.position, Quaternion.identity);
            }

            // Notify level manager of completion
            // TODO: Implement reward/progression system
        }

        private class CrystalInteractable : InteractableObject
        {
            private LightCrystalPuzzle puzzle;
            private CrystalNode node;

            public void Initialize(LightCrystalPuzzle puzzle, CrystalNode node)
            {
                this.puzzle = puzzle;
                this.node = node;
            }

            protected override void OnInteract()
            {
                var character = Core.GameManager.Instance.currentCharacter;
                if (character != null)
                {
                    puzzle.OnCrystalInteraction(node, character.characterType);
                }
            }
        }
    }
} 