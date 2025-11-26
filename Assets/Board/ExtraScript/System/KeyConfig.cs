using UnityEngine;
using System;

[CreateAssetMenu(fileName = "NewKeyConfig", menuName = "Resource/KeyConfig")]
public class KeyConfig : ScriptableObject
{
    // system
    public string TRANSLATION_DEFAULT;

    // tag
    public string TAG_PLAYER;
    public string TAG_MAIN_CAMERA;
    public string TAG_MAIN_CANVAS;
    public string TAG_MAIN_MESSGAE;
    public string TAG_MAIN_MAP;
    public string TAG_CURTAIN_LEFT;
    public string TAG_CURTAIN_ROOF;
    public string TAG_CURTAIN_RIGHT;
    public string TAG_CURTAIN_FLOOR;

    // attribute
    public string ATTRIBUTE_FRAME;
    public string ATTRIBUTE_WALL_FLOOR;
    public string ATTRIBUTE_WALL_LEFT;
    public string ATTRIBUTE_WALL_RIGHT;
    public string ATTRIBUTE_WALL_ROOF;
    public string ATTRIBUTE_TITLE;
    public string ATTRIBUTE_VEIL;
    public string ATTRIBUTE_LETTERBOX_LEFT;
    public string ATTRIBUTE_LETTERBOX_TOP;
    public string ATTRIBUTE_LETTERBOX_RIGHT;
    public string ATTRIBUTE_LETTERBOX_BOTTOM;
    public string ATTRIBUTE_MESSAGE;
    public string ATTRIBUTE_TRIGGER;
    public string ATTRIBUTE_MESSENGER;
    public string ATTRIBUTE_CONTENT;
    public string ATTRIBUTE_ELEVATOR_LEFT;
    public string ATTRIBUTE_ELEVATOR_RIGHT;

    // state id
    public string STATE_ID_IDLE;
    public string STATE_ID_WALK;
    public string STATE_ID_RUN;

    // extra
    public string EXTRA_COLLISION_ENTITIES;
    public string EXTRA_COLLIDER_ENTITIES;

    // command type
    public string COMMAND_TYPE_MOVEMENT;
    public string COMMAND_TYPE_SETTING;
    public string COMMAND_TYPE_FRAME;
    public string COMMAND_TYPE_BORDER;
    public string COMMAND_TYPE_SCENE;

    // parameter
    public enum DIRECTION
    {
        LEFT, TOP, RIGHT, BOTTOM
    }

    // message type
    public string MESSAGE_TYPE_NORMAL;

    // feature
    public string FEATURE_KEY_LOAD;
    public string FEATURE_KEY_SCENE;
    public string FEATURE_KEY_START_SCENE;
    public string FEATURE_KEY_END_SCENE;
    public string FEATURE_KEY_START_ELEVATOR;
    public string FEATURE_KEY_END_ELEVATOR;
    public string FEATURE_KEY_THROUGH;

    public string FEATURE_VALUE_CHANGE;
    public string FEATURE_VALUE_FADE_IN;
    public string FEATURE_VALUE_FADE_OUT;
    public string FEATURE_VALUE_ELEVATOR;
    public string FEATURE_VALUE_PLAYER_POSITION;
}