using UnityEngine;

namespace LittleEmber.World
{
    /// <summary>
    /// Simple villager: idles, then wanders to a random point near home, using
    /// 3-frame walk cycles per direction (pack chara sheets: 4 dirs x 3 frames).
    /// Frame order per direction array: walk-a, stand, walk-b (classic layout).
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(SpriteRenderer))]
    public class VillagerNpc : MonoBehaviour
    {
        public Sprite[] down = new Sprite[0];
        public Sprite[] left = new Sprite[0];
        public Sprite[] right = new Sprite[0];
        public Sprite[] up = new Sprite[0];

        [Header("Wander")]
        public float speed = 1.1f;
        public float wanderRadius = 3f;
        public Vector2 pauseRange = new Vector2(1.5f, 4.5f);
        public float frameRate = 7f;

        /// <summary>Freeze in place (dialogue, cutscenes). Shows the stand frame.</summary>
        public bool paused;

        Rigidbody2D rb;
        SpriteRenderer sr;
        Vector2 home;
        Vector2 target;
        bool moving;
        float pauseTimer;
        float animTimer;
        int frame;
        Sprite[] currentFrames;
        Vector2 lastPos;
        float stuckTimer;

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            sr = GetComponent<SpriteRenderer>();
            home = transform.position;
            currentFrames = down.Length > 0 ? down : right;
            lastPos = rb.position;
            pauseTimer = Random.Range(0.5f, 2.5f);
            Show(1); // stand frame
        }

        void FixedUpdate()
        {
            if (paused)
            {
                if (moving) { moving = false; Show(1); }
                return;
            }
            if (!moving)
            {
                pauseTimer -= Time.fixedDeltaTime;
                if (pauseTimer <= 0f) PickTarget();
                return;
            }

            Vector2 pos = rb.position;
            Vector2 to = target - pos;
            if (to.magnitude < 0.15f)
            {
                StopMoving();
                return;
            }

            Vector2 dir = to.normalized;
            rb.MovePosition(pos + dir * speed * Time.fixedDeltaTime);
            PickDirection(dir);

            // stuck against something? give up and repick
            stuckTimer = (pos - lastPos).sqrMagnitude < 0.00001f ? stuckTimer + Time.fixedDeltaTime : 0f;
            lastPos = pos;
            if (stuckTimer > 0.8f) StopMoving();
        }

        void Update()
        {
            if (!moving || currentFrames.Length < 3) { if (!moving) Show(1); return; }
            animTimer += Time.deltaTime;
            if (animTimer >= 1f / frameRate)
            {
                animTimer = 0f;
                frame = (frame + 1) % 3; // 0,1,2 with 1 = stand
                Show(frame);
            }
        }

        void PickTarget()
        {
            Vector2 offset = Random.insideUnitCircle * wanderRadius;
            target = home + offset;
            moving = true;
            stuckTimer = 0f;
            frame = 0;
        }

        void StopMoving()
        {
            moving = false;
            pauseTimer = Random.Range(pauseRange.x, pauseRange.y);
            Show(1);
        }

        void PickDirection(Vector2 dir)
        {
            if (Mathf.Abs(dir.y) >= Mathf.Abs(dir.x))
                currentFrames = dir.y > 0 ? (up.Length > 0 ? up : down) : down;
            else
                currentFrames = dir.x < 0 ? (left.Length > 0 ? left : right) : right;
        }

        void Show(int i)
        {
            if (currentFrames == null || currentFrames.Length == 0) return;
            sr.sprite = currentFrames[Mathf.Clamp(i, 0, currentFrames.Length - 1)];
        }
    }
}
