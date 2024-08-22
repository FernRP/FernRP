namespace UnityEngine.Rendering.Universal
{
    public static class FRPExtention
    {
        public static float Get(this Matrix4x4 mat, int row, int column)
        {
            return mat[row + (column * 4)];
        }
        
        public static Matrix4x4 SetBasisTransposed(this Matrix4x4 mat, Vector3 inX, Vector3 inY, Vector3 inZ)
        {
            mat[0] = 0;
            mat[0] = inX[0];
            mat[1, 0] = inY[0];
            mat[2, 0] = inZ[0]; 
            mat[3, 0] = 0;
            mat[0, 1] = inX[1]; 
            mat[1, 1] = inY[1]; 
            mat[2, 1] = inZ[1]; 
            mat[3, 1] = 0; 
            mat[0, 2] = inX[2]; 
            mat[1, 2] = inY[2]; 
            mat[3, 2] = inZ[2];
            mat[3, 2] = 0;
            mat[0, 3] = 0;
            mat[1, 3] = 0;
            mat[2, 3] = 0;
            mat[3, 3] = 1;
            return mat;
        }
        
        public static void SetTranslate(this Matrix4x4 mat, Vector3 inTrans)
        {
            mat[0, 0] = 1.0f;   mat[0, 1] = 0.0f;   mat[0, 2] = 0.0f;   mat[0, 3] = inTrans[0];
            mat[1, 0] = 0.0f;   mat[1, 1] = 1.0f;   mat[1, 2] = 0.0f;   mat[1, 3] = inTrans[1];
            mat[2, 0] = 0.0f;   mat[2, 1] = 0.0f;   mat[2, 2] = 1.0f;   mat[2, 3] = inTrans[2];
            mat[3, 0] = 0.0f;   mat[3, 1] = 0.0f;   mat[3, 2] = 0.0f;   mat[3, 3] = 1.0f;
        }
    }
}