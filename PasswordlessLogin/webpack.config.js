const path = require('path')
const VueLoaderPlugin = require('vue-loader/lib/plugin')
const WebpackCleanupPlugin = require('webpack-cleanup-plugin')

module.exports = {
  entry: {
    bundle: './src/main.js'
  },
  output: {
    filename: 'bundle.js',
    chunkFilename: '[name].[chunkhash:12].js',
    path: path.resolve(__dirname, 'wwwroot/passwordless/dist'),
    publicPath: '/passwordless.dist.'
  },
  module: {
    rules: [
      { test: /\.vue$/, include: /src/, loader: 'vue-loader' },
      { 
        test: /\.js$/, 
        loader: 'babel-loader',
        exclude: /node_modules/, 
        options: {
          babelrc: false,
          presets: [['es2015', { modules: false }], 'stage-3']
        }
      }
    ]
  },
  plugins: [
    new VueLoaderPlugin(),
    new WebpackCleanupPlugin()
  ],
  /*optimization: {
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
  },*/
  devtool: '#source-map'
};