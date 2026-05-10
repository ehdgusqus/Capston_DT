using UnityEngine;

public class HardwareTagFollower : MonoBehaviour
{
    [Header("하드웨어 통신 데이터")]
    public Vector3 targetPosition; // 태그에서 수신받은 최신 목표 좌표

    [Header("부드러운 이동 설정")]
    public float moveSpeed = 10f;    // 목표 위치를 따라붙는 속도 (수치가 클수록 딱딱하게 붙음)
    public float rotationSpeed = 15f; // 캐릭터가 이동 방향으로 고개를 돌리는 속도

    private Animator _animator;
    
    // StarterAssets 애니메이션 조작을 위한 ID 캐싱
    private int _animIDSpeed;
    private int _animIDMotionSpeed;
    private int _animIDGrounded;

    void Start()
    {
        _animator = GetComponent<Animator>();
        
        // 문자열로 애니메이터를 조작하면 느리므로 Hash ID로 변환해둡니다.
        _animIDSpeed = Animator.StringToHash("Speed");
        _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
        _animIDGrounded = Animator.StringToHash("Grounded");

        // 허공에서 허우적거리지 않도록 무조건 땅에 닿아있다고 설정
        if (_animator != null) _animator.SetBool(_animIDGrounded, true);

        // 게임 시작 시 목표 위치를 현재 위치로 동기화
        targetPosition = transform.position; 
    }

    void Update()
    {
        // 1. 이동 방향과 거리 계산
        Vector3 direction = targetPosition - transform.position;
        direction.y = 0; // 위아래(높이)로 고개가 꺾이는 현상 방지
        
        float distance = direction.magnitude;

        // 2. 부드러운 위치 이동 (Lerp 보간법)
        // 현재 위치에서 목표 위치(targetPosition)를 향해 부드럽게 미끄러지듯 이동합니다.
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * moveSpeed);

        // 3. 이동하는 방향을 자연스럽게 바라보도록 회전
        if (distance > 0.05f) // 제자리에 있을 때는 덜덜거리지 않게 회전 정지
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
        }

        // 4. 애니메이션 재생 (걷기/뛰기 모션)
        if (_animator != null)
        {
            // 목표점까지의 거리가 멀수록 애니메이션 속도(Speed)를 높여서 뛰게 만들고,
            // 목표점에 다다르면(거리가 0에 가까워지면) 자동으로 멈춤(Idle) 모션이 나옵니다.
            float animationSpeed = Mathf.Clamp(distance * 5f, 0f, 6f); 
            
            _animator.SetFloat(_animIDSpeed, animationSpeed);
            _animator.SetFloat(_animIDMotionSpeed, 1f); // 재생 배율
        }
    }

    // 💡 중요: 하드웨어에서 새로운 좌표 신호가 들어올 때마다 외부 스크립트에서 이 함수를 실행해주세요!
    public void UpdateHardwarePosition(Vector3 newTagPosition)
    {
        // 들어온 좌표를 즉시 적용하지 않고 '목표 좌표'로만 갱신합니다.
        targetPosition = newTagPosition; 
    }
}