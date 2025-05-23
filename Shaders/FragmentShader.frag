#version 330 core
out vec4 FragColor;

in vec2 TexCoords;

uniform sampler2D texture0;
uniform bool useTexture;
uniform vec4 objectColor;

void main()
{
    if (useTexture) {
        FragColor = texture(texture0, TexCoords);
    } else {
        FragColor = objectColor;
    }
}