void DrawSkeleton(ImDrawListPtr drawList, Entity entity, Matrix4x4 camMatrix, float screenWidth, float screenHeight)
{
    bool W2S(Vector3 worldPos, out Vector2 screenPos)
    {
        screenPos = Core.W2S(camMatrix, worldPos, screenWidth, screenHeight);
        return screenPos.X > 0 && screenPos.X < screenWidth && screenPos.Y > 0 && screenPos.Y < screenHeight;
    }

    float dist = entity.DistanceToPlayer;
    uint boneColor = dist < 50f
        ? ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 0f, 0f, 1f))
        : dist < 100f
            ? ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 0.65f, 0f, 1f))
            : ImGui.ColorConvertFloat4ToU32(new Vector4(0f, 1f, 0f, 1f));

    uint lineColor = ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 1f, 1f, 0.65f));
    float radius = 3.0f;

    // ─── Đường nối tạo khung xương hoàn chỉnh ───
    //
    //         [head]
    //           |
    //        [Spine]
    //       /       \
    // [LShoulder] [RShoulder]
    //      |             |
    //  [LElbow]      [RElbow]
    //      |             |
    //  [LWrist]      [RWrist]
    //      |             |
    //  [LHand]       [RHand]
    //        \       /
    //         [root]
    //       /       \
    //   [LHip]     [RHip]
    //      |             |
    //  [LKnee]       [RKnee]
    //      |             |
    // [LAnkle]      [RAnkle]
    //      |             |
    //  [LFoot]       [RFoot]

    var connections = new (Vector3 from, Vector3 to)[]
    {
        // Cột sống trung tâm
        (entity.head,          entity.Spine),
        (entity.Spine,         entity.root),

        // Vai trái → Spine
        (entity.Spine,         entity.LeftShoulder),
        // Vai phải → Spine
        (entity.Spine,         entity.RightShoulder),

        // Cánh tay trái
        (entity.LeftShoulder,  entity.LeftElbow),
        (entity.LeftElbow,     entity.LeftWrist),
        (entity.LeftWrist,     entity.LeftHand),

        // Cánh tay phải
        (entity.RightShoulder, entity.RightElbow),
        (entity.RightElbow,    entity.RightWrist),
        (entity.RightWrist,    entity.RightHand),

        // Hông nối root
        (entity.root,          entity.LeftHip),
        (entity.root,          entity.RightHip),

        // Chân trái
        (entity.LeftHip,       entity.LeftKnee),
        (entity.LeftKnee,      entity.LeftAnkle),
        (entity.LeftAnkle,     entity.LeftFoot),

        // Chân phải
        (entity.RightHip,      entity.RightKnee),
        (entity.RightKnee,     entity.RightAnkle),
        (entity.RightAnkle,    entity.RightFoot),
    };

    // Vẽ đường nối
    foreach (var (from, to) in connections)
    {
        if (W2S(from, out Vector2 a) && W2S(to, out Vector2 b))
            drawList.AddLine(a, b, lineColor, 1.5f);
    }

    // Vẽ điểm khớp (circle)
    var joints = new Vector3[]
    {
        entity.head,
        entity.Spine,          entity.root,
        entity.LeftShoulder,   entity.RightShoulder,
        entity.LeftElbow,      entity.RightElbow,
        entity.LeftWrist,      entity.RightWrist,
        entity.LeftHand,       entity.RightHand,
        entity.LeftHip,        entity.RightHip,
        entity.LeftKnee,       entity.RightKnee,
        entity.LeftAnkle,      entity.RightAnkle,
        entity.LeftFoot,       entity.RightFoot,
    };

    foreach (var joint in joints)
    {
        if (W2S(joint, out Vector2 screenPos))
            drawList.AddCircleFilled(screenPos, radius, boneColor);
    }
}