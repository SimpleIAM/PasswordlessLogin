const path = require('path')
const VueLoaderPlugin = require('vue-loader/lib/plugin')

module.exports = {
  entry: {
    signin: './UI/EntryPoints/SignIn.js',
    account: './UI/EntryPoints/Account.js'
  },
  output: {
    filename: '[name].bundle.js',
    path: path.resolve(__dirname, 'wwwroot')
  },
  module: {
    rules: [
        { test: /\.vue$/, include: /UI/, loader: 'vue-loader' }
    ]
  },
  plugins: [
    new VueLoaderPlugin()
  ],
  optimization: {
      splitChunks: {
          cacheGroups: {
            common: {
              name: 'common',
              chunks: 'all',
              minChunks: 2,
              enforce: true
            }
          },
      },
      runtimeChunk: false
  },
  devtool: '#source-map'
};