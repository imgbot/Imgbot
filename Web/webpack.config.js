const path = require('path')
const webpack = require('webpack')
const { VueLoaderPlugin } = require('vue-loader')

module.exports = {
  entry: './src/app/vue/main.js',
  output: {
    path: path.resolve(__dirname, './dist/app'),
    filename: 'index.js'
  },
  module: {
    rules: [
      {
        test: /\.js$/,
        exclude: /(node_modules|bower_components)/,
        use: 'babel-loader'
      },
      {
        test: /\.vue$/,
        loader: 'vue-loader'
      },
      {
        test: /\.css$/,
        use: ['vue-style-loader', 'css-loader']
      }
    ]
  },
  resolve: {
    alias: {
      vue$: 'vue/dist/vue.esm.js'
    },
    extensions: ['*', '.js', '.vue', '.json']
  },
  plugins: [
    new VueLoaderPlugin(),
    new webpack.IgnorePlugin(/^\.\/locale$/, /moment$/)
  ]
}
