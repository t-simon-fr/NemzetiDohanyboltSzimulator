using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Object = UnityEngine.Object;

public static class PlayerMovementChecks
{
    public static object Run()
    {
        int passed = 0;
        Action<bool, string> check = (ok, name) => { if (!ok) throw new Exception(name); passed++; };
        var game = MvpShopController.Instance;
        var player = Object.FindAnyObjectByType<FirstPersonController>();
        var body = player.GetComponent<CharacterController>();
        Vector3 home = player.transform.position;
        Quaternion rotation = player.transform.rotation;
        var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "Movement check floor";
        floor.transform.position = new Vector3(100, -.1f, 100);
        floor.transform.localScale = new Vector3(24, .2f, 24);
        var ceiling = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ceiling.transform.position = new Vector3(100, 2.2f, 100);
        ceiling.transform.localScale = new Vector3(24, .2f, 24);
        ceiling.SetActive(false);
        var origin = new Vector3(100, .1f, 100);
        Action reset = () => {
            body.enabled = false;
            player.transform.SetPositionAndRotation(origin, Quaternion.identity);
            body.enabled = true;
            Physics.SyncTransforms();
            for (int i = 0; i < 50; i++) player.StepMovement(Vector2.zero, false, false, .02f);
        };
        game.ManualTick = true;
        try
        {
            game.ResetDemo();
            reset();
            check(player.IsGrounded, "Real controller settles on floor");
            Vector3 start = player.transform.position;
            for (int i = 0; i < 25; i++) player.StepMovement(Vector2.up, false, false, .02f);
            float walked = player.transform.position.z - start.z;
            check(walked > 1.65f && walked < 1.85f, "Walking keeps existing 3.5 metre speed");
            reset(); start = player.transform.position;
            for (int i = 0; i < 25; i++) player.StepMovement(Vector2.up, true, false, .02f);
            float sprinted = player.transform.position.z - start.z;
            check(sprinted > walked * 1.6f && sprinted < walked * 1.7f, "Shift sprint is faster without changing base speed");
            reset(); start = player.transform.position;
            for (int i = 0; i < 25; i++) player.StepMovement(Vector2.one, true, false, .02f);
            Vector3 delta = player.transform.position - start; delta.y = 0;
            check(Mathf.Abs(delta.magnitude - sprinted) < .03f, "Diagonal sprint has no speed boost");
            reset(); float ground = player.transform.position.y;
            player.StepMovement(Vector2.zero, false, true, .02f);
            check(!player.IsGrounded && player.VerticalSpeed > 0, "Space starts a grounded jump");
            float velocity = player.VerticalSpeed;
            player.StepMovement(Vector2.zero, false, true, .02f);
            check(player.VerticalSpeed < velocity, "Second airborne press cannot add jump impulse");
            float peak = player.transform.position.y;
            for (int i = 0; i < 80; i++) { player.StepMovement(Vector2.zero, false, false, .02f); peak = Mathf.Max(peak, player.transform.position.y); }
            check(peak - ground > .5f && peak - ground < .8f, "Jump reaches configured safe height");
            check(player.IsGrounded && Mathf.Abs(player.transform.position.y - ground) < .03f, "Gravity returns to same floor");
            ceiling.SetActive(true); reset();
            for (int i = 0; i < 80; i++) player.StepMovement(Vector2.zero, false, i == 0, .02f);
            check(player.IsGrounded && player.transform.position.y < .2f, "Low ceiling does not trap or launch player");
            start = player.transform.position;
            ShopInputGate.Blocked = true;
            player.StepMovement(Vector2.up, true, true, .5f);
            check(Vector3.Distance(start, player.transform.position) < .001f, "Device input gate blocks sprint and jump");
            ShopInputGate.Blocked = false;
            game.GetComponent<ShopMenu>().Open();
            player.StepMovement(Vector2.up, true, true, .5f);
            check(Vector3.Distance(start, player.transform.position) < .001f, "Paused menu blocks movement and jump");
            game.GetComponent<ShopMenu>().Resume();
            check(ShopBindings.Actions.Length == 14 && ShopBindings.Actions[12].Key == Key.LeftShift && ShopBindings.Actions[13].Key == Key.Space, "Both new actions are configurable");
            return new { passed, failed = 0 };
        }
        finally
        {
            ShopInputGate.Blocked = false;
            game.GetComponent<ShopMenu>().Resume();
            floor.GetComponent<Collider>().enabled = false;
            ceiling.GetComponent<Collider>().enabled = false;
            body.enabled = false;
            player.transform.SetPositionAndRotation(home, rotation);
            body.enabled = true;
            Physics.SyncTransforms();
            Object.Destroy(floor); Object.Destroy(ceiling);
            game.ResetDemo(); game.ManualTick = false;
        }
    }
}
