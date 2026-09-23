using System;
using TankGame.Gameplay;
using UnityEngine;
using UnityEngine.InputSystem;
namespace TankGame.Input
{
    public sealed class HumanTankController : ITankController, IDisposable
    {
        readonly InputAction move;
        readonly InputAction fire;
        readonly InputAction mine;
        readonly Camera camera;
        readonly Plane plane;
        readonly AimMemory aim;
        readonly FirePressGate fireGate = new FirePressGate();
        public HumanTankController(Camera camera, float planeHeight, Vector3 initialDirection)
        {
            this.camera = camera; plane = new Plane(Vector3.up, new Vector3(0, planeHeight, 0)); aim = new AimMemory(initialDirection);
            move = new InputAction("Move", InputActionType.Value);
            move.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s").With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            fire = new InputAction("Fire", InputActionType.Button, "<Mouse>/leftButton");
            mine = new InputAction("Place Mine", InputActionType.Button, "<Keyboard>/q");
            move.Enable(); fire.Enable(); mine.Enable();
        }
        public TankCommand ReadCommand(in TankObservation observation)
        {
            if (!Application.isFocused) return new TankCommand(Vector3.zero, aim.Resolve(observation.Position, null), false);
            var mouse = Mouse.current;
            bool inside = false;
            Vector3? hitPoint = null;
            if (mouse != null)
            {
                Vector2 cursor = mouse.position.ReadValue();
                inside = camera.pixelRect.Contains(cursor);
                if (inside && plane.Raycast(camera.ScreenPointToRay(cursor), out float distance))
                    hitPoint = camera.ScreenPointToRay(cursor).GetPoint(distance);
            }
            Vector2 input = Vector2.ClampMagnitude(move.ReadValue<Vector2>(), 1);
            Vector3 right = Vector3.ProjectOnPlane(camera.transform.right, Vector3.up).normalized;
            Vector3 up = Vector3.ProjectOnPlane(camera.transform.up, Vector3.up).normalized;
            bool fireRequested = fireGate.Read(fire.WasPressedThisFrame(), fire.IsPressed());
            return new TankCommand(right * input.x + up * input.y, aim.Resolve(observation.Position, hitPoint),
                inside && fireRequested, mine.WasPressedThisFrame());
        }
        public void Dispose() { move.Dispose(); fire.Dispose(); mine.Dispose(); }
    }
}
