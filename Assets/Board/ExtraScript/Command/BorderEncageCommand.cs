using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class BorderEncageCommand : ICommand
{
    private string _commandId;
    private StaticClock _clock;

    private string _leftAttribute;
    private string _topAttribute;
    private string _rightAttribute;
    private string _bottomAttribute;

    public delegate float RangeGetter(GameObjectContext context);

    private RangeGetter GetLeftWidth;
    private RangeGetter GetTopHeight;
    private RangeGetter GetRightWidth;
    private RangeGetter GetBottomHeight;

    public BorderEncageCommand(string commandId, string leftAttribute, string topAttribute, string rightAttribute, string bottomAttribute,
        RangeGetter GetLeftWidth, RangeGetter GetTopHeight, RangeGetter GetRightWidth, RangeGetter GetBottomHeight)
    {
        _commandId = commandId;
        _leftAttribute = leftAttribute;
        _topAttribute = topAttribute;
        _rightAttribute = rightAttribute;
        _bottomAttribute = bottomAttribute;
        this.GetLeftWidth = GetLeftWidth;
        this.GetTopHeight = GetTopHeight;   
        this.GetRightWidth = GetRightWidth;
        this.GetBottomHeight = GetBottomHeight;
    }

    // ICommand
    public string CommandId
    {
        get { return _commandId; }
    }
    public int Execute(GameObjectContext context)
    {
        ColliderEntities colliderEntities = null;
        if (context.TryGetExtraData<ColliderEntities>(GlobalConfig.Key.EXTRA_COLLIDER_ENTITIES, out colliderEntities) == false)
        {
            GlobalLogger.Error(context.gameObject.name, _commandId, nameof(Execute), "context.TryGetExtraData<ColliderEntities>(GlobalConfig.Key.EXTRA_COLLIDER_ENTITIES, out colliderEntities) == false");
            return CommandQueue.ERROR;
        }
        if (colliderEntities == null)
        {
            GlobalLogger.Error(context.gameObject.name, _commandId, nameof(Execute), "colliderEntities == null");
            return CommandQueue.ERROR;
        }

        Transform transform = context.gameObject.transform;

        foreach (var colliderEntity in colliderEntities.EntitySet)
        {
            Vector3 colliderPosition = colliderEntity.transform.position;
            Vector3 colliderScale = colliderEntity.transform.localScale;
            if (Util.ContainAttribute(colliderEntity, _leftAttribute))
            {
                Vector3 position = transform.position;
                position.x = colliderPosition.x + GetLeftWidth(context) + colliderScale.x / 2f;
                transform.position = position;
            }
            if (Util.ContainAttribute(colliderEntity, _topAttribute))
            {
                Vector3 position = transform.position;
                position.y = colliderPosition.y - GetTopHeight(context) - colliderScale.y / 2f;
                transform.position = position;
            }
            if (Util.ContainAttribute(colliderEntity, _rightAttribute))
            {
                Vector3 position = transform.position;
                position.x = colliderPosition.x - GetRightWidth(context) - colliderScale.x / 2f;
                transform.position = position;
            }
            if (Util.ContainAttribute(colliderEntity, _bottomAttribute))
            {
                Vector3 position = transform.position;
                position.y = colliderPosition.y + GetBottomHeight(context) + colliderScale.y / 2f;
                transform.position = position;
            }
        }

        return CommandQueue.PROCESS;
    }
}