using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Reflection;
using Linq.Flickr.Attribute;
using System.Collections;
using System.Collections.ObjectModel;
using Linq.Flickr.Interface;

namespace Linq.Flickr
{
    [Serializable]
    public class FlickrQuery : Photos, IQueryable<Photo>, IQueryProvider
    {
        private Expression _expression = null;
        private IList<Photo> _photos = new List<Photo>();
        private Photo _dummyPhotoObject = null;
        
        private int _itemsToSkip = 0;
        private int _itemsToTake = 100;
        private bool _getRecent = false;
       
        public FlickrQuery()
        {
            _dummyPhotoObject = new Photo();
        }

        private void ProcessExpression(Expression expression, Photo photo)
        {
            if (expression.NodeType == ExpressionType.Equal)
            {
                this.ProcessBinaryResult((BinaryExpression)expression, ExpressionType.Equal, photo);
            }
            if (expression.NodeType == ExpressionType.AndAlso)
            {
                this.ProcessExpression(((BinaryExpression)expression).Left, photo);
                this.ProcessExpression(((BinaryExpression)expression).Right, photo);
            }
            if (expression.NodeType == ExpressionType.OrElse)
            {
                throw new ApplicationException("OR expresstion not supported yet");
            }
            if (expression.NodeType == ExpressionType.LessThan)
            {
                this.ProcessBinaryResult((BinaryExpression)expression, ExpressionType.LessThan, photo);
            }
            if (expression is UnaryExpression)
            {
                UnaryExpression uExp = expression as UnaryExpression;
                ProcessExpression(uExp.Operand, photo);
            }
            else if (expression is LambdaExpression)
            {
                ProcessExpression(((LambdaExpression)expression).Body, photo);
            }

            
            else if (expression is ParameterExpression)
            {
                if (((ParameterExpression)expression).Type == typeof(Photo))
                {
                    _getRecent = true;   
                }
            }
        }


        private void ProcessBinaryResult(BinaryExpression expression, ExpressionType expressionType, Photo bucket)
        {
            if (expression.Left is MemberExpression)
            {
                ExtractDataFromExpression(bucket, expression.Left, expression.Right);//expression.Left);
            }
            else
            {
                // this is needed for enum comparsion , when Convert(ph.something) is used.
                if (expression.Left is UnaryExpression)
                {
                    UnaryExpression uExp = (UnaryExpression)expression.Left;
                    ExtractDataFromExpression(bucket, uExp.Operand, expression.Right);
                }
            }
        }

        private void ExtractDataFromExpression(Photo bucket, Expression left , Expression right)
        {
            Expression rightExpression = right;

            MemberExpression memberExpression = (MemberExpression)left;
            string originalMembername = memberExpression.Member.Name;
            string callingMemberName = string.Empty;
            if (right is MemberExpression)
            {
                callingMemberName = ((MemberExpression)right).Member.Name;
            }
            else if (right is UnaryExpression)
            {
                UnaryExpression uRight = (UnaryExpression)right;
                callingMemberName = ((MemberExpression)uRight.Operand).Member.Name;
            }

            // find leaf
            while (true)
            {
                if (rightExpression is ConstantExpression)
                    break;

                if (rightExpression is MemberExpression)
                {
                    if (((MemberExpression)rightExpression).Expression != null)
                    {
                        rightExpression = ((MemberExpression)rightExpression).Expression;
                    }
                    else
                    {
                        break;
                    }
                }
                else if (rightExpression is UnaryExpression)
                {
                    rightExpression = ((UnaryExpression)rightExpression).Operand;
                }
            }
            
            
            PropertyInfo[] infos = bucket.GetType().GetProperties();

            foreach (PropertyInfo info in infos)
            {

                if (string.Compare(info.Name, originalMembername, false) == 0)
                {
                    object[] attr = info.GetCustomAttributes(typeof(UseInExpressionAttribute), false);

                    if (attr != null && attr.Length > 0)
                    {
                        UseInExpressionAttribute useAttribute = attr[0] as UseInExpressionAttribute;

                        if (!useAttribute.Supported)
                        {
                            throw new ApplicationException(info.Name + " " + "not supported in expression");
                        }
                    }

                    if (info.CanWrite)
                    {
                        if (rightExpression.NodeType == ExpressionType.Constant)
                        {
                            ConstantExpression constExp = ((ConstantExpression)rightExpression);
                            if (constExp.Type.IsPrimitive)
                            {
                                object value = constExp.Value;
                                info.SetValue(bucket, value, null);
                            }
                            else
                            {
                                ProcessMemberAccess(callingMemberName, (ConstantExpression)rightExpression, bucket, info);
                            }
                        }
                        else if (rightExpression.NodeType == ExpressionType.MemberAccess)
                        {
                            ProcessMemberAccess(callingMemberName, rightExpression, bucket, info);
                        }
                    }
                }
            }
        }

        private bool IsDefaultType(string name)
        {
            bool defautlType = false;

            switch (name)
            {
                case "System.String":
                    defautlType = true;
                    break;
                case "System.Int32":
                    defautlType = true;
                    break;
                case "System.Boolean":
                    defautlType = true;
                    break;
                case "System.DateTime":
                    defautlType = true;
                    break;
                default:
                    defautlType = false;
                    break;
            }
            return defautlType;
        }

        private void ProcessMemberAccess(string compareField, Expression expression, Photo bucket, PropertyInfo info)
        {
            bool isSet = false;

            if (expression is ConstantExpression)
            {
                ConstantExpression constExpr = (ConstantExpression)expression;

                object value = constExpr.Value;

                if (value != null)
                {
                    Type type = value.GetType();

                    if (!IsDefaultType(type.FullName))
                    {
                        PropertyInfo[] pInfos = type.GetProperties();

                        foreach (PropertyInfo pInfo in pInfos)
                        {
                            // check if we, got the field we are looking for.
                            if (pInfo.Name.Contains(compareField))
                            {
                                isSet = true;
                                info.SetValue(bucket, pInfo.GetValue(value, null), null);
                                break;
                            }
                        }
                        FieldInfo[] fInfos = type.GetFields();

                        foreach (FieldInfo fInfo in fInfos)
                        {
                            // check if we, got the field we are looking for.
                            if (fInfo.Name.Contains(compareField))
                            {
                                isSet = true;
                                info.SetValue(bucket, fInfo.GetValue(value), null);
                                break;
                            }
                        }
                    }
                    else
                    {
                        isSet = true;
                        info.SetValue(bucket, value, null);
                    }

                }
            }
            // true for static variable access
            else if (expression is MemberExpression)
            {
                MemberExpression memberExpr = (MemberExpression)expression;

                Type reflectedType = memberExpr.Member.ReflectedType;

                FieldInfo[] fInfos = reflectedType.GetFields(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.DeclaredOnly | BindingFlags.Public);

                foreach (FieldInfo fInfo in fInfos)
                {
                    if (fInfo.Name.Contains(compareField))
                    {
                        isSet = true;
                        info.SetValue(bucket, fInfo.GetValue(null), null);
                    }
                }

                PropertyInfo[] pInfos = reflectedType.GetProperties(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.DeclaredOnly | BindingFlags.Public);

                foreach (PropertyInfo pInfo in pInfos)
                {
                    if (pInfo.Name.Contains(compareField))
                    {
                        isSet = true;
                        info.SetValue(bucket, pInfo.GetValue(null, null), null);
                    }
                }
            }

            if (!isSet)
            {
                throw new ApplicationException("Expression format used for: " + compareField + " is not supportd");
            }
        }

        internal void SubmitChanges()
        {
            using (IFlickr flickr = new DataAccess())
            {
                foreach (Photo photo in this.Items)
                {
                    if (photo.IsNew)
                    {
                        flickr.Upload(photo);
                    }

                    if (photo.IsDeleted)
                    {
                        flickr.Delete(photo.Id);
                        this.Remove(photo);
                    }
                }
            }
        }


        private void ProcessItem(Photo bucket, ExpressionType expType)
        {
            this.Clear();

            using (IFlickr flickr = new DataAccess())
            {
                if (!string.IsNullOrEmpty(bucket.Id))
                {
                    Photo photo = flickr.GetPhotoDetail(bucket.Id, bucket.PhotoSize);

                    if (photo != null)
                    {
                         this.Clear();
                         this.Add(photo);
                    }
                    
                }
                else
                {
                    int index = _itemsToSkip + 1;
                    if (index == 0) index = index + 1;

                    bool authenticate = false;
                    string token = string.Empty;
                    // for private or semi-private photo do authenticate.
                    if (bucket.ViewMode != ViewMode.Public)
                    {
                        authenticate = true;
                    }
                  
                    if (authenticate)
                    {
                        token = flickr.Authenticate(authenticate);
                    }

                    // addition to parameterless search, if there is no token and searchtext , get recent photos.
                    if (string.IsNullOrEmpty(token) && string.IsNullOrEmpty(bucket.SearchText))
                    {
                        _getRecent = true;
                    }

                    // if authenticated call, without params , then get my photos.
                    if (!string.IsNullOrEmpty(token) && _getRecent)
                    {
                        bucket.ViewMode = ViewMode.Owner;
                    }

                    if (!string.IsNullOrEmpty(token) || (!_getRecent))
                    {
                        this.AddRange(flickr.Search(bucket.User, bucket.SearchText, bucket.PhotoSize, bucket.ViewMode, bucket.SortOrder, index, _itemsToTake, SearchMode.OR));
                    }
                    else
                    {

                       this.AddRange(flickr.GetRecent(index, _itemsToTake, bucket.PhotoSize));
                    }
                }
            }
        }


        #region IEnumerable<Photo> Members

        public IEnumerator<Photo> GetEnumerator()
        {
            return (this as IQueryable).Provider.Execute<IList<Photo>>(_expression).GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return (IEnumerator<Photo>)(this as IQueryable).GetEnumerator();
        }

        #endregion

        #region IQueryable Members

        public Type ElementType
        {
            get { return typeof(Photo); }
        }

        public System.Linq.Expressions.Expression Expression
        {
            get { return Expression.Constant(this); }
        }

        public IQueryProvider Provider
        {
            get { return this; }
        }

        #endregion

        #region IQueryProvider Members

        internal class CallType
        {
            public const string TAKE = "Take";
            public const string SKIP = "Skip";
            public const string WHERE = "Where";
            public const string SELECT = "Select";
        }

        public IQueryable<S> CreateQuery<S>(System.Linq.Expressions.Expression expression)
        {
            if (typeof(S) != typeof(Photo))
                throw new Exception("Only " + typeof(Photo).FullName + " objects are supported.");

            this._expression = expression;

            MethodCallExpression methodcall = _expression as MethodCallExpression;

            if (methodcall.Method.Name == CallType.TAKE)
            {
               _itemsToTake = (int)((ConstantExpression)methodcall.Arguments[1]).Value;
            }
            else if (methodcall.Method.Name == CallType.SKIP)
            {
                _itemsToSkip = (int)((ConstantExpression)methodcall.Arguments[1]).Value;
            }
            else
            {
                ProcessExpression(methodcall.Arguments[1], _dummyPhotoObject);
            }
            
            return (IQueryable<S>)((ConstantExpression) methodcall.Arguments[0]).Value;
        }

        public IQueryable CreateQuery(System.Linq.Expressions.Expression expression)
        {
            return (IQueryable<Photo>)(this as IQueryProvider).CreateQuery<Photo>(expression);
        }

        public TResult Execute<TResult>(System.Linq.Expressions.Expression expression)
        {
            return (TResult)(this as IQueryProvider).Execute(expression);
        }
        
        public object Execute(System.Linq.Expressions.Expression expression)
        {
            ProcessItem(_dummyPhotoObject, ExpressionType.Equal);

            if (expression is MethodCallExpression)
            {
                MethodCallExpression mCallExp = (MethodCallExpression)expression;
                // when first , last or single is called 
                if (mCallExp.Method.ReturnType == typeof(Photo))
                {
                    Type itemType = this.GetType();
                    string methodName = mCallExp.Method.Name;

                    MethodInfo[] mInfos = itemType.GetMethods();
                    foreach (MethodInfo mInfo in mInfos)
                    {
                        if (string.Compare(methodName, mInfo.Name, false) == 0)
                        {
                           return itemType.InvokeMember(methodName, BindingFlags.InvokeMethod, null, this, null);   
                        }
                    }
                }

            }
    
            return Items;
        }


        #endregion
    }
}
