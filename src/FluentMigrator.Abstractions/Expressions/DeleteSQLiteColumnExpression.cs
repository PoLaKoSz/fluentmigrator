#region License
//
// Copyright (c) 2007-2018, Sean Chambers <schambers80@gmail.com>
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//   http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
#endregion

using System.Collections.Generic;

namespace FluentMigrator.Expressions
{
    public class DeleteSQLiteColumnExpression
    {
        /// <summary>
        /// Wrapped Expression to store table name and the
        /// columns which should be deleted
        /// </summary>
        public DeleteColumnExpression GenericExpression { get; }

        /// <summary>
        /// Name of all available colums in the table
        /// </summary>
        public ICollection<string> AvailableColumnNames { get; set; }

        public DeleteSQLiteColumnExpression(DeleteColumnExpression expression)
        {
            GenericExpression = expression;
            AvailableColumnNames = new List<string>();
        }
    }
}
